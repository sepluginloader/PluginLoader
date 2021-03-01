using avaness.PluginLoader.Data;
using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.World;
using SpaceEngineers.Game;
using System;
using System.Linq;
using System.Reflection;
using VRage.Plugins;

namespace avaness.PluginLoader
{
    public class PluginInstance
    {
        private readonly LogFile log;
        private readonly Type mainType;
        private readonly PluginData data;
        private readonly Harmony harmony;
        private readonly Assembly mainAssembly;
        private IPlugin plugin;

        private PluginInstance(Harmony harmony, LogFile log, PluginData data, Assembly mainAssembly, Type mainType)
        {
            this.harmony = harmony;
            this.log = log;
            this.data = data;
            this.mainAssembly = mainAssembly;
            this.mainType = mainType;
        }

        public bool Instantiate()
        {
            try
            {
                plugin = (IPlugin)Activator.CreateInstance(mainType);
                return true;
            }
            catch (Exception e) 
            {
                ThrowError($"Failed to instantiate {data} because of an error: {e}");
                return false;
            }
        }

        public bool Init(object gameInstance)
        {
            if (plugin == null)
                return false;

            try
            {

                if (plugin is SEPluginManager.SEPMPlugin sepm)
                    LoaderTools.ExecuteMain(log, sepm);
                plugin.Init(gameInstance);
                return true;
            }
            catch (Exception e)
            {
                ThrowError($"Failed to initialize {data} because of an error: {e}");
                return false;
            }
        }

        public void RegisterSession()
        {
            if (plugin != null)
                MySession.Static.RegisterComponentsFromAssembly(mainAssembly, true);
        }

        public bool Update()
        {
            if (plugin == null)
                return false;

            try
            {
                plugin.Update();

                if (data.FriendlyName.Contains("World"))
                    throw new Exception();

                return true;
            }
            catch (Exception e)
            {
                ThrowError($"Failed to update {data} because of an error: {e}");
                return false;
            }
        }

        public void Dispose(bool error = false)
        {
            if(plugin != null)
            {
                try
                {
                    plugin.Dispose();
                    plugin = null;
                    if (error)
                    {
                        log.WriteLine($"Attempting to remove because {data} because it is not working properly...");
                        if(LoaderTools.UnpatchAll(harmony, mainAssembly))
                            log.WriteLine("Unpatched harmony patches.");
                        LoaderTools.ResetGameSettings();
                        log.WriteLine("Reset MyPerGameSettings.");
                    }
                }
                catch (Exception e)
                {
                    data.Status = PluginStatus.Error;
                    log.WriteLine($"Failed to dispose {data} because of an error: {e}");
                }
            }
        }


        public static bool TryGet(Harmony harmony, LogFile log, PluginData data, out PluginInstance instance)
        {
            instance = null;
            if (!data.TryLoadAssembly(log, out Assembly a))
                return false;

            Type pluginType = a.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t));
            if (pluginType == null)
            {
                data.Status = PluginStatus.Error;
                log.WriteLine($"Failed to load {data} because it does not contain an IPlugin.");
                return false;
            }

            instance = new PluginInstance(harmony, log, data, a, pluginType);
            return true;
        }

        public override string ToString()
        {
            return data.ToString();
        }

        private void ThrowError(string error)
        {
            data.Status = PluginStatus.Error;
            log.WriteLine(error);
            Dispose(true);
        }
    }
}
