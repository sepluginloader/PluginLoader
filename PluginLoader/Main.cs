using VRage.Plugins;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;
using VRage.FileSystem;
using HarmonyLib;
using System.Windows.Forms;
using Sandbox.Game.World;
using System.Diagnostics;
using System.Linq;
using avaness.PluginLoader.Compiler;
using avaness.PluginLoader.GUI;
using avaness.PluginLoader.Data;
using avaness.PluginLoader.Stats;

namespace avaness.PluginLoader
{
    public class Main : IHandleInputPlugin
    {
        public static Main Instance;

        private static string PluginsDir => pluginsDirCache ??= Path.GetFullPath(Path.Combine(MyFileSystem.ExePath, "Plugins"));
        private static string pluginsDirCache;

        public PluginList List { get; }
        public PluginConfig Config { get; }
        public SplashScreen Splash { get; }

        private bool init;

        private readonly List<PluginInstance> plugins = new List<PluginInstance>();

        public Main()
        {
            Stopwatch sw = Stopwatch.StartNew();

            Splash = new SplashScreen();

            Instance = this;

            Cursor temp = Cursor.Current;
            Cursor.Current = Cursors.AppStarting;

            Directory.CreateDirectory(PluginsDir);

            LogFile.Init(PluginsDir);
            LogFile.WriteLine("Starting");

            Splash.SetText("Finding references...");
            RoslynReferences.GenerateAssemblyList();

            AppDomain.CurrentDomain.AssemblyResolve += ResolveDependencies;

            Config = PluginConfig.Load(PluginsDir);
            List = new PluginList(PluginsDir, Config);

            Config.Init(List);

            Splash.SetText("Patching...");
            LogFile.WriteLine("Patching");
            new Harmony("avaness.PluginLoader").PatchAll(Assembly.GetExecutingAssembly());

            Splash.SetText("Instantiating plugins...");
            LogFile.WriteLine("Instantiating plugins");
            foreach (string id in Config)
            {
                PluginData data = List[id];
                if (data is GitHubPlugin github)
                    github.Init(PluginsDir);
                if (PluginInstance.TryGet(data, out PluginInstance p))
                    plugins.Add(p);
            }

            sw.Stop();

            // FIXME: It can potentially run in the background speeding up the game's startup
            ReportEnabledPlugins();

            LogFile.WriteLine($"Finished startup. Took {sw.ElapsedMilliseconds}ms");

            Cursor.Current = temp;

            Splash.Delete();
            Splash = null;
        }


        private void ReportEnabledPlugins()
        {
            if (!PlayerConsent.ConsentGiven)
                return;

            Splash.SetText("Reporting plugin usage...");
            LogFile.WriteLine("Reporting plugin usage");

            // Config has already been validated at this point so all enabled plugins will have list items
            // FIXME: Move into a background thread
            if (StatsClient.Track(TrackablePluginIds))
                LogFile.WriteLine("List of enabled plugins has been sent to the statistics server");
            else
                LogFile.WriteLine("Failed to send the list of enabled plugins to the statistics server");
        }

        // Skip local plugins, keep only enabled ones
        public string[] TrackablePluginIds => Config.EnabledPlugins.Where(id => !List[id].IsLocal).ToArray();

        public void RegisterComponents()
        {
            LogFile.WriteLine("Registering components");
            foreach (PluginInstance plugin in plugins)
                plugin.RegisterSession(MySession.Static);
        }

        public void DisablePlugins()
        {
            Config.Disable();
            plugins.Clear();
            LogFile.WriteLine("Disabled all plugins");
        }

        public void InstantiatePlugins()
        {
            LogFile.WriteLine($"Loading {plugins.Count} plugins");
            for (int i = plugins.Count - 1; i >= 0; i--)
            {
                PluginInstance p = plugins[i];
                if (!p.Instantiate())
                    plugins.RemoveAtFast(i);
            }
        }

        public void Init(object gameInstance)
        {
            LogFile.WriteLine($"Initializing {plugins.Count} plugins");
            for (int i = plugins.Count - 1; i >= 0; i--)
            {
                PluginInstance p = plugins[i];
                if (!p.Init(gameInstance))
                    plugins.RemoveAtFast(i);
            }
            init = true;
        }

        public void Update()
        {
            if (init)
            {
                for (int i = plugins.Count - 1; i >= 0; i--)
                {
                    PluginInstance p = plugins[i];
                    if (!p.Update())
                        plugins.RemoveAtFast(i);
                }
            }
        }

        public void HandleInput()
        {
            if (init)
            {
                for (int i = plugins.Count - 1; i >= 0; i--)
                {
                    PluginInstance p = plugins[i];
                    if (!p.HandleInput())
                        plugins.RemoveAtFast(i);
                }
            }
        }

        public void Dispose()
        {
            foreach (PluginInstance p in plugins)
                p.Dispose();
            plugins.Clear();

            AppDomain.CurrentDomain.AssemblyResolve -= ResolveDependencies;
            LogFile.Dispose();
            Instance = null;
        }


        private Assembly ResolveDependencies(object sender, ResolveEventArgs args)
        {
            string assembly = args.RequestingAssembly?.GetName().ToString();
            if (args.Name.Contains("0Harmony"))
            {
                if (assembly != null)
                    LogFile.WriteLine("Resolving 0Harmony for " + assembly);
                else
                    LogFile.WriteLine("Resolving 0Harmony");
                return typeof(Harmony).Assembly;
            }
            else if (args.Name.Contains("SEPluginManager"))
            {
                if (assembly != null)
                    LogFile.WriteLine("Resolving SEPluginManager for " + assembly);
                else
                    LogFile.WriteLine("Resolving SEPluginManager");
                return typeof(SEPluginManager.SEPMPlugin).Assembly;
            }

            return null;
        }
    }
}