using avaness.PluginLoader.Data;
using HarmonyLib;
using Sandbox;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VRage.Plugins;

namespace avaness.PluginLoader
{
    public class SessionPlugins
    {
        private readonly List<Assembly> pluginAssemblies = new List<Assembly>();
        private readonly List<IPlugin> plugins = new List<IPlugin>();
        private readonly LogFile log;
        private readonly PluginConfig config;
        private readonly Harmony harmony;

        public SessionPlugins(LogFile log, PluginConfig config, Harmony harmony)
        {
            this.log = log;
            this.config = config;
            this.harmony = harmony;
            MySession.BeforeLoading += MySession_BeforeLoading;
            MySession.OnLoading += MySession_OnLoading;
            MySession.OnUnloading += MySession_OnUnloading;
            MySession.OnUnloaded += MySession_OnUnloaded;
        }

        public void Unload()
        {
            MySession.BeforeLoading -= MySession_BeforeLoading;
            MySession.OnLoading -= MySession_OnLoading;
            MySession.OnUnloading -= MySession_OnUnloading;
            MySession.OnUnloaded -= MySession_OnUnloaded;
        }

        public void Update()
        {
            foreach (IPlugin plugin in plugins)
                plugin.Update();
        }

        private void MySession_OnUnloaded()
        {
            log.WriteLine("Unloaded");
            DisposePlugins();
        }

        private void MySession_OnUnloading()
        {
            log.WriteLine("Unloading");
            DisposePlugins();
        }

        private void DisposePlugins()
        {
            foreach (IPlugin plugin in plugins)
                plugin.Dispose();
            plugins.Clear();
            UnpatchAll();
            pluginAssemblies.Clear();
        }

        private void UnpatchAll()
        {
            foreach (MethodBase patched in Harmony.GetAllPatchedMethods())
            {
                Patches patches = Harmony.GetPatchInfo(patched);
                if(patches != null)
                {
                    foreach (Assembly a in pluginAssemblies)
                        UnpatchAll(patched, patches, a);
                }
            }
        }

        private void UnpatchAll(MethodBase original, Patches patches, Assembly patchOwner)
        {
            UnpatchAll(original, patches.Prefixes, patchOwner);
            UnpatchAll(original, patches.Postfixes, patchOwner);
            UnpatchAll(original, patches.Transpilers, patchOwner);
            UnpatchAll(original, patches.Finalizers, patchOwner);
        }

        private void UnpatchAll(MethodBase original, IEnumerable<HarmonyLib.Patch> patches, Assembly patchOwner)
        {
            foreach(HarmonyLib.Patch p in patches)
            {
                MethodInfo method = p.PatchMethod;
                if (method.DeclaringType.Assembly == patchOwner)
                {
                    log.WriteLine($"Unpatching {original} that was patched by {patchOwner}...");
                    harmony.Unpatch(original, method);
                }
            }
        }

        private void MySession_BeforeLoading()
        {
            log.WriteLine("Before Loading");
            CreatePlugins();
            InitPlugins();
        }

        private void MySession_OnLoading()
        {
            log.WriteLine("Loading");
            RegisterPlugins();
        }

        private void RegisterPlugins()
        {
            foreach (Assembly a in pluginAssemblies)
                MySession.Static.RegisterComponentsFromAssembly(a);
        }

        private void InitPlugins()
        {
            object gameInstance = MySandboxGame.Static;
            foreach (IPlugin plugin in plugins)
                plugin.Init(gameInstance);
        }

        private void CreatePlugins()
        {
            plugins.Clear();

            HashSet<ulong> modIds = new HashSet<ulong>();
            foreach(var mod in MySession.Static.Mods)
            {
                if (mod.PublishedServiceName == "Steam")
                    modIds.Add(mod.PublishedFileId);
            }

            foreach(PluginData data in config.Data.Values)
            {
                if(data is SteamPlugin steam && !data.Enabled && modIds.Contains(steam.WorkshopId))
                {
                    var result = MessageBox.Show($"This world has the plugin {data.FriendlyName}, load it?", "Plugin Loader", MessageBoxButtons.YesNo);
                    if(result == DialogResult.Yes)
                    {
                        log.WriteLine("Loading " + data);
                        try
                        {
                            if (data.LoadDll(log, out IPlugin plugin))
                            {
                                plugins.Add(plugin);
                                pluginAssemblies.Add(plugin.GetType().Assembly);
                                if (plugin is SEPluginManager.SEPMPlugin sepm)
                                    Main.Instance?.ExecuteMain(sepm);
                            }
                            else
                            {
                                log.WriteLine("Unable to load session plugin " + data);
                            }
                        }
                        catch (Exception e)
                        {
                            log.WriteLine("An error occurred:\n" + e);
                        }

                    }
                }
            }
        }
    }
}
