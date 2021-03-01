using avaness.PluginLoader.Data;
using HarmonyLib;
using Sandbox.Game.World;
using System;
using System.Linq;
using System.Reflection;
using VRage.Plugins;

namespace avaness.PluginLoader
{
    public class PluginInstance
    {
        private const int errorLimit = 10;

        private readonly LogFile log;
        private readonly Type mainType;
        private readonly PluginData data;
        private readonly Harmony harmony;
        private readonly Assembly mainAssembly;
        private IPlugin plugin;
        private int errors;

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
                log.WriteLine($"Failed to instantiate {data} because of an error: {e}");
            }
            return false;
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
                log.WriteLine($"Failed to initialize {data} because of an error: {e}");
            }
            return false;
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
                return true;
            }
            catch (Exception e)
            {
                errors++;
                log.WriteLine($"Failed to update {data} ({errors}/{errorLimit}) because of an error: {e}");
            }
            return errors < errorLimit;
        }

        public void Dispose(bool unpatch = false)
        {
            if(plugin != null)
            {
                try
                {
                    plugin.Dispose();
                    if(unpatch)
                        LoaderTools.UnpatchAll(harmony, mainAssembly);
                }
                catch (Exception e)
                {
                    log.WriteLine($"Failed to dispose {data} because of an error: {e}");
                }
            }
        }

        public void MarkBad()
        {
            data.Status = PluginStatus.Error;
            log.WriteLine($"{data} was disabled because it was not working properly.");
            Dispose(true);
        }

        public static bool TryGet(Harmony harmony, LogFile log, PluginData data, out PluginInstance instance)
        {
            instance = null;
            if (!data.TryLoadAssembly(log, out Assembly a))
                return false;

            foreach(var name in a.GetReferencedAssemblies())
            {
                log.WriteLine(name.ToString());
                if(!LoaderTools.CheckAssemblyRef(name))
                {
                    data.Status = PluginStatus.Error;
                    log.WriteLine($"Failed to load {data} because it uses an assembly '{name.FullName}' that is not allowed!");
                    return false;
                }

            }

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
    }
}
