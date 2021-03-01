using avaness.PluginLoader.Data;
using HarmonyLib;
using Sandbox;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Plugins;
using System.Windows.Forms;
using System.Linq;
using Sandbox.Game.Gui;

namespace avaness.PluginLoader.Session
{
    public class SessionPlugins
    {
        private readonly List<SessionPlugin> plugins = new List<SessionPlugin>();
        private readonly LogFile log;
        private readonly PluginConfig config;
        private readonly Harmony harmony;
        private GameState state;

        public SessionPlugins(LogFile log, PluginConfig config, Harmony harmony)
        {
            this.log = log;
            this.config = config;
            this.harmony = harmony;
            MySession.BeforeLoading += MySession_BeforeLoading;
            MySession.OnLoading += MySession_OnLoading;
            MySession.OnUnloading += MySession_OnUnloading;
            MySession.OnUnloaded += MySession_OnUnloaded;
            // TODO multiplayer sessions
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
            foreach (SessionPlugin plugin in plugins)
                plugin.Update();
        }

        private void MySession_OnUnloaded()
        {
            DisposePlugins();
            if(state != null)
            {
                state.Apply();
                state = null;
            }
        }

        private void MySession_OnUnloading()
        {
            DisposePlugins();
        }

        private void DisposePlugins()
        {
            if(plugins.Count > 0)
            {
                log.WriteLine("Unloading session plugins.");
                foreach (SessionPlugin plugin in plugins)
                    plugin.Unload();
                LoaderTools.UnpatchAll(harmony, plugins.Select(p => p.PluginAssembly));
                plugins.Clear();
            }
        }

        private void MySession_BeforeLoading()
        {
            if (CreatePlugins())
                state = new GameState();
        }

        private void MySession_OnLoading()
        {
            foreach (SessionPlugin plugin in plugins)
                plugin.Register();
        }


        private bool CreatePlugins()
        {
            bool result = false;

            plugins.Clear();

            HashSet<ulong> modIds = new HashSet<ulong>();
            foreach(var mod in MySession.Static.Mods)
            {
                if (mod.PublishedFileId > 0 && mod.PublishedServiceName == "Steam")
                    modIds.Add(mod.PublishedFileId);
            }

            config.CheckForNewMods(modIds);

            log.WriteLine("Checking for session plugins...");

            foreach(PluginData data in config.Data.Values)
            {
                if(data is SteamPlugin steam && !data.Enabled && modIds.Contains(steam.WorkshopId))
                {
                    if(MessageBox.Show(LoaderTools.GetMainForm(), $"This world has the plugin {data.FriendlyName}, load it?", "Plugin Loader", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        log.WriteLine($"Loading {data} for session.");
                        try
                        {
                            if (SessionPlugin.TryGet(log, data, out SessionPlugin plugin))
                            {
                                plugins.Add(plugin);
                                result = true;
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

            return result;
        }
    }
}
