using VRage.Plugins;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System;
using System.IO;
using avaness.PluginLoader.Data;
using SEPluginManager;
using VRage.FileSystem;
using HarmonyLib;
using System.Windows.Forms;
using avaness.PluginLoader.Session;

namespace avaness.PluginLoader
{
    public class Main : IPlugin
    {
        public static Main Instance;
        
        public PluginConfig Config { get; }

        private readonly string mainPath;
        private LogFile log;
        private SessionPlugins session;

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

            AppDomain.CurrentDomain.AssemblyResolve += ResolveHarmony;

            log.WriteLine("Loading config.");
            Config = PluginConfig.Load(mainPath, log);

            Harmony harmony = new Harmony("avaness.PluginLoader");
            harmony.PatchAll();

            List<Assembly> assemblies = new List<Assembly>();
            bool error = false;
            foreach (PluginData data in Config.Data.Values)
            {
                try
                {
                    if(data.Enabled)
                    {
                        log.WriteLine($"Loading {data}");
                        if (data.LoadDll(log, out Assembly a))
                        {
                            assemblies.Add(a);
                        }
                        else
                        {
                            error = true;
                            data.Status = PluginStatus.Error;
                        }
                    }
                    else
                    {
                        log.WriteLine($"Skipped {data}");
                    }
                }
                catch (Exception e)
                {
                    log.WriteLine("An error occurred:\n" + e);
                    data.Status = PluginStatus.Error;
                }
            }

            if(assemblies.Count > 0)
            {
                log.WriteLine($"Linking {assemblies.Count} assemblies to the game.");
                MethodInfo loadPlugins = typeof(MyPlugins).GetMethod("LoadPlugins", BindingFlags.NonPublic | BindingFlags.Static);
                try
                {
                    loadPlugins.Invoke(null, new object[] { assemblies });
                    ScanForSEPM();
                }
                catch(TargetInvocationException e)
                {
                    StringBuilder sb = new StringBuilder("An error occurred:");
                    sb.AppendLine();
                    ReflectionTypeLoadException inner = e.InnerException as ReflectionTypeLoadException;
                    if (inner == null)
                    {
                        sb.Append(e).AppendLine();
                    }
                    else
                    {
                        foreach (Exception le in inner.LoaderExceptions)
                            sb.Append(le).AppendLine();
                    }
                    log.WriteLine(sb.ToString());
                    error = true;
                }
                catch (Exception e)
                {
                    log.WriteLine("An error occurred:\n" + e);
                    error = true;
                }

            }
            else
            {
                log.WriteLine("No assemblies to link!");
            }

            log.WriteLine("Finished startup.");

            log.Flush();

            Cursor.Current = temp;

            session = new SessionPlugins(log, Config, harmony);

            if (error)
                MessageBox.Show(LoaderTools.GetMainForm(), $"There was an error while trying to load a plugin. Some or all of the plugins may not have been loaded. See loader.log or the game log for details.", "Plugin Loader", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ScanForSEPM()
        {
            foreach(IPlugin p in MyPlugins.Plugins)
            {
                if (p is SEPluginManager.SEPMPlugin sepm)
                    LoaderTools.ExecuteMain(log, sepm);
            }
        }

        private Assembly ResolveHarmony(object sender, ResolveEventArgs args)
        {
            string assembly = args.RequestingAssembly?.GetName()?.ToString();
            if(args.Name.Contains("0Harmony"))
            {
                if(assembly != null)
                    log.WriteLine("Resolving 0Harmony for " + assembly);
                else
                    log.WriteLine("Resolving 0Harmony");
                return typeof(Harmony).Assembly;
            }
            else if(args.Name.Contains("SEPluginManager"))
            {
                if (assembly != null)
                    log.WriteLine("Resolving SEPluginManager for " + assembly);
                else
                    log.WriteLine("Resolving SEPluginManager");
                return typeof(Main).Assembly;
            }
            return null;
        }

        public void Init(object gameInstance)
        { }

        public void Update()
        {
            session?.Update();
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveHarmony;
            Instance = null;
            log?.Dispose();
            log = null;
            session?.Unload();
        }
    }
}
