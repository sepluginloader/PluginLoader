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
using avaness.PluginLoader.Compiler;
using avaness.PluginLoader.GUI;

namespace avaness.PluginLoader
{
    public class Main : IPlugin
    {
        public static Main Instance;

        public PluginList List { get; }
        public PluginConfig Config { get; }
        public SplashScreenLabel Label { get; private set; }

        private bool init;

        private readonly List<PluginInstance> plugins = new List<PluginInstance>();

        public Main()
        {
            Label = new SplashScreenLabel();

            Stopwatch sw = Stopwatch.StartNew();

            Instance = this;

            Cursor temp = Cursor.Current;
            Cursor.Current = Cursors.AppStarting;

            string mainPath = Path.GetFullPath(Path.Combine(MyFileSystem.ExePath, "Plugins"));
            if (!Directory.Exists(mainPath))
                Directory.CreateDirectory(mainPath);

            LogFile.Init(mainPath);
            LogFile.WriteLine("Starting.");

            RoslynReferences.GenerateAssemblyList();

            AppDomain.CurrentDomain.AssemblyResolve += ResolveDependencies;

            Config = PluginConfig.Load(mainPath);
            List = new PluginList(mainPath, Config);

            LogFile.WriteLine("Loading config.");
            Config.Init(List);

            Harmony harmony = new Harmony("avaness.PluginLoader");
            harmony.PatchAll();

            Label.SetText("Instantiating plugins...");
            foreach (string id in Config)
            {
                if (PluginInstance.TryGet(List[id], out PluginInstance p))
                    plugins.Add(p);
            }

            sw.Stop();

            LogFile.WriteLine($"Finished startup. Took {sw.ElapsedMilliseconds}ms");
            LogFile.Flush();

            Cursor.Current = temp;

            Label.SetText("Done.");
        }


        public void RegisterComponents()
        {
            LogFile.WriteLine("Registering Components...");
            foreach (PluginInstance plugin in plugins)
                plugin.RegisterSession(MySession.Static);
            LogFile.Flush();
        }

        public void DisablePlugins()
        {
            Config.Disable();
            plugins.Clear();
            LogFile.WriteLine("Disabled all plugins.");
            LogFile.Flush();
        }

        public void InstantiatePlugins()
        {
            LogFile.WriteLine($"Loading {plugins.Count} plugins...");
            for (int i = plugins.Count - 1; i >= 0; i--)
            {
                PluginInstance p = plugins[i];
                if (!p.Instantiate())
                    plugins.RemoveAtFast(i);
            }

            LogFile.Flush();
        }

        public void Init(object gameInstance)
        {
            Label.Delete();
            Label = null;

            LogFile.WriteLine($"Initializing {plugins.Count} plugins...");
            for (int i = plugins.Count - 1; i >= 0; i--)
            {
                PluginInstance p = plugins[i];
                if(!p.Init(gameInstance))
                    plugins.RemoveAtFast(i);
            }
            LogFile.Flush();
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
