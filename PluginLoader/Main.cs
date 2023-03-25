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
using System.Net;
using avaness.PluginLoader.Network;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using avaness.PluginLoader.Stats.Model;
using avaness.PluginLoader.Config;

namespace avaness.PluginLoader
{
    public class Main : IHandleInputPlugin
    {
        const string HarmonyVersion = "2.2.2.0";

        public static Main Instance;

        public PluginList List { get; }
        public PluginConfig Config { get; }
        public SplashScreen Splash { get; }
        public PluginStats Stats {get; private set; }

        public NuGetClient NuGet { get; } = new NuGetClient();

        /// <summary>
        /// True if a local plugin was loaded
        /// </summary>
        public bool HasLocal { get; private set; }

        private bool init;

        private readonly List<PluginInstance> plugins = new List<PluginInstance>();

        public Main()
        {
            Stopwatch sw = Stopwatch.StartNew();

            Splash = new SplashScreen();

            Instance = this;

            Cursor temp = Cursor.Current;
            Cursor.Current = Cursors.AppStarting;

            string pluginsDir = LoaderTools.PluginsDir;
            Directory.CreateDirectory(pluginsDir);

            LogFile.Init(pluginsDir);
            LogFile.WriteLine("Starting - v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

            NuGet.Init();
            GitHub.Init();

            Splash.SetText("Finding references...");
            RoslynReferences.GenerateAssemblyList();

            AppDomain.CurrentDomain.AssemblyResolve += ResolveDependencies;
            AppDomain.CurrentDomain.FirstChanceException += OnException;


            Splash.SetText("Starting...");
            Config = PluginConfig.Load(pluginsDir);
            Config.CheckGameVersion();
            List = new PluginList(pluginsDir, Config);
            
            Splash.SetText("Starting...");
            Config.Init(List);

            if (Config.GameVersionChanged)
                ClearGitHubCache(pluginsDir);

            StatsClient.OverrideBaseUrl(Config.StatsServerBaseUrl);
            UpdatePlayerStats();
            PlayerConsent.OnConsentChanged += OnConsentChanged;

            Splash.SetText("Patching...");
            LogFile.WriteLine("Patching");

            // Check harmony version
            Version expectedHarmony = new Version(HarmonyVersion);
            Version actualHarmony = typeof(Harmony).Assembly.GetName().Version;
            if (expectedHarmony != actualHarmony)
                LogFile.WriteLine($"WARNING: Unexpected Harmony version, plugins may be unstable. Expected {expectedHarmony} but found {actualHarmony}");

            new Harmony("avaness.PluginLoader").PatchAll(Assembly.GetExecutingAssembly());

            Splash.SetText("Instantiating plugins...");
            LogFile.WriteLine("Instantiating plugins");
            foreach (PluginData data in Config.EnabledPlugins)
            {
                if (PluginInstance.TryGet(data, out PluginInstance p))
                {
                    plugins.Add(p);
                    if (data.IsLocal)
                        HasLocal = true;
                }
            }

            sw.Stop();

            // FIXME: It can potentially run in the background speeding up the game's startup
            ReportEnabledPlugins();

            LogFile.WriteLine($"Finished startup. Took {sw.ElapsedMilliseconds}ms");

            Cursor.Current = temp;

            Splash.Delete();
            Splash = null;

        }
        
        private void OnException(object sender, FirstChanceExceptionEventArgs e)
        {
            try
            {
                if (e.Exception is MemberAccessException accessException)
                {
                    foreach (PluginInstance plugin in plugins)
                    {
                        if (plugin.ContainsExceptionSite(accessException))
                            return;
                    }
                }
            }
            catch { } // Do NOT throw exceptions inside this method!
        }
        
        public void UpdatePlayerStats()
        {
            LogFile.WriteLine("Downloading user statistics", false);
            ParallelTasks.Parallel.Start(() =>
            {
                Stats = StatsClient.DownloadStats();
            });
        }

        private void ClearGitHubCache(string pluginsDir)
        {
            string pluginCache = Path.Combine(pluginsDir, "GitHub");
            if (!Directory.Exists(pluginCache))
                return;

            bool hasGitHub = Config.EnabledPlugins.Any(x => x is GitHubPlugin);

            if(hasGitHub)
            {
                MessageBox.Show(LoaderTools.GetMainForm(), "Space Engineers has been updated, so all plugins that are currently enabled must be downloaded and compiled.", "PluginLoader", MessageBoxButtons.OK);
            }

            try
            {
                Directory.Delete(pluginCache);
            }
            catch { }
        }

        public bool TryGetPluginInstance(string id, out PluginInstance instance)
        {
            instance = null;
            if (!init)
                return false;

            foreach (PluginInstance p in plugins)
            {
                if (p.Id == id)
                {
                    instance = p;
                    return true;
                }
            }

            return false;
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
        public string[] TrackablePluginIds => Config.EnabledPlugins.Where(x => !x.IsLocal).Select(x => x.Id).ToArray();

        public void RegisterComponents()
        {
            LogFile.WriteLine($"Registering {plugins.Count} components");
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

            PlayerConsent.OnConsentChanged -= OnConsentChanged;
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveDependencies;
            LogFile.Dispose();
            Instance = null;
        }

        private void OnConsentChanged()
        {
            UpdatePlayerStats();
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

            return null;
        }
    }
}