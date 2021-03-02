using avaness.PluginLoader.Data;
using Sandbox.Game.World;
using System;
using System.Linq;
using System.Reflection;
using VRage.Game.Components;
using VRage.Plugins;

namespace avaness.PluginLoader
{
    public class PluginInstance
    {
        private readonly LogFile log;
        private readonly Type mainType;
        private readonly PluginData data;
        private readonly Assembly mainAssembly;
        private IPlugin plugin;

        private PluginInstance(LogFile log, PluginData data, Assembly mainAssembly, Type mainType)
        {
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

        public void RegisterSession(MySession session)
        {
            if (plugin != null)
            {
                try
                {
                    Type descType = typeof(MySessionComponentDescriptor);
                    int count = 0;
                    foreach (Type t in mainAssembly.GetTypes().Where(t => Attribute.IsDefined(t, descType)))
                    {
                        MySessionComponentBase comp = (MySessionComponentBase)Activator.CreateInstance(t);
                        session.RegisterComponent(comp, comp.UpdateOrder, comp.Priority);
                        count++;
                    }
                    if(count > 0)
                        log.WriteLine($"Registered {count} session components from: {mainAssembly.FullName}");
                }
                catch (Exception e)
                {
                    ThrowError($"Failed to register {data} because of an error: {e}");
                }
            }
                
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
                ThrowError($"Failed to update {data} because of an error: {e}");
                return false;
            }
        }

        public void Dispose()
        {
            if(plugin != null)
            {
                try
                {
                    plugin.Dispose();
                    plugin = null; 
                }
                catch (Exception e)
                {
                    data.Status = PluginStatus.Error;
                    log.WriteLine($"Failed to dispose {data} because of an error: {e}");
                }
            }
        }

        private void ThrowError(string error)
        {
            log.WriteLine(error);
            log.Flush();
            data.Error();
            Dispose();
        }

        public static bool TryGet(LogFile log, PluginData data, out PluginInstance instance)
        {
            instance = null;
            if (!data.TryLoadAssembly(log, out Assembly a))
                return false;

            Type pluginType = a.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t));
            if (pluginType == null)
            {
                log.WriteLine($"Failed to load {data} because it does not contain an IPlugin.");
                log.Flush();
                data.Error();
                return false;
            }

            instance = new PluginInstance(log, data, a, pluginType);
            return true;
        }

        public override string ToString()
        {
            return data.ToString();
        }

    }
}
