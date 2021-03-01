using VRage.Plugins;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System;
using System.IO;
using avaness.PluginLoader.Data;
using VRage.FileSystem;
using HarmonyLib;
using System.Windows.Forms;
using Sandbox.Game.World;

namespace avaness.PluginLoader
{
    public class Main : IPlugin
    {
        public static Main Instance;
        
        public PluginConfig Config { get; }

        private readonly string mainPath;
        private LogFile log;
        private bool loadingErrors, init;

        private readonly List<PluginInstance> plugins = new List<PluginInstance>();

        public Main()
        {
            Instance = this;

            Cursor temp = Cursor.Current;
            Cursor.Current = Cursors.AppStarting;

            mainPath = Path.GetFullPath(Path.Combine(MyFileSystem.ExePath, "Plugins"));
            if (!Directory.Exists(mainPath))
                Directory.CreateDirectory(mainPath);

            log = new LogFile(mainPath);
            log.WriteLine("Starting.");

            AppDomain.CurrentDomain.AssemblyResolve += ResolveDependencies;

            log.WriteLine("Loading config.");
            Config = PluginConfig.Load(mainPath, log);

            Harmony harmony = new Harmony("avaness.PluginLoader");
            harmony.PatchAll();

            foreach (PluginData data in Config.Data.Values)
            {
                if (data.Enabled)
                {
                    if (PluginInstance.TryGet(harmony, log, data, out PluginInstance p))
                    {
                        plugins.Add(p);
                    }
                    else
                    {
                        loadingErrors = true;
                        data.Status = PluginStatus.Error;
                    }
                }
            }

            log.WriteLine("Finished startup.");
            log.Flush();

            Cursor.Current = temp;

            AppDomain.CurrentDomain.AssemblyResolve -= ResolveDependencies;
            MySession.OnLoading += MySession_OnLoading;
        }

        private void MySession_OnLoading()
        {
            foreach (PluginInstance plugin in plugins)
                plugin.RegisterSession();
        }

        public void DisablePlugins()
        {
            Config.Disable();
            plugins.Clear();
            log.WriteLine("Disabled all plugins.");
            log.Flush();
        }

        public void InstantiatePlugins()
        {
            log.WriteLine($"Loading {plugins.Count} plugins...");
            for (int i = plugins.Count - 1; i >= 0; i--)
            {
                PluginInstance p = plugins[i];
                if (!p.Instantiate())
                {
                    plugins.RemoveAtFast(i);
                    loadingErrors = true;
                }
            }

            if (loadingErrors)
                MessageBox.Show(LoaderTools.GetMainForm(), $"There was an error while trying to load a plugin. Some or all of the plugins may not have been loaded. See loader.log or the game log for details.", "Plugin Loader", MessageBoxButtons.OK, MessageBoxIcon.Error);

            log.Flush();
        }

        public void Init(object gameInstance)
        {
            log.WriteLine($"Initializing {plugins.Count} plugins...");
            for (int i = plugins.Count - 1; i >= 0; i--)
            {
                PluginInstance p = plugins[i];
                if(!p.Init(gameInstance))
                    plugins.RemoveAtFast(i);
            }
            log.Flush();
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
            MySession.OnLoading -= MySession_OnLoading;
            log?.Dispose();
            log = null;
            Instance = null;
        }


        private Assembly ResolveDependencies(object sender, ResolveEventArgs args)
        {
            string assembly = args.RequestingAssembly?.GetName()?.ToString();
            if (args.Name.Contains("0Harmony"))
            {
                if (assembly != null)
                    log.WriteLine("Resolving 0Harmony for " + assembly);
                else
                    log.WriteLine("Resolving 0Harmony");
                return typeof(Harmony).Assembly;
            }
            else if (args.Name.Contains("SEPluginManager"))
            {
                if (assembly != null)
                    log.WriteLine("Resolving SEPluginManager for " + assembly);
                else
                    log.WriteLine("Resolving SEPluginManager");
                return typeof(SEPluginManager.SEPMPlugin).Assembly;
            }
            return null;
        }
    }
}
