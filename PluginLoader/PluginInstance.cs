using avaness.PluginLoader.Data;
using Sandbox.Game.World;
using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using VRage.Game.Components;
using VRage.Plugins;

namespace avaness.PluginLoader
{
    public class PluginInstance
    {
        private readonly Type mainType;
        private readonly PluginData data;
        private readonly Assembly mainAssembly;
        private MethodInfo openConfigDialog;
        private IPlugin plugin;
        private IHandleInputPlugin inputPlugin;

        public string Id => data.Id;
        public bool HasConfigDialog => openConfigDialog != null;

        private PluginInstance(PluginData data, Assembly mainAssembly, Type mainType)
        {
            this.data = data;
            this.mainAssembly = mainAssembly;
            this.mainType = mainType;
        }

        public bool Instantiate()
        {
            try
            {
                plugin = (IPlugin)Activator.CreateInstance(mainType);
                inputPlugin = plugin as IHandleInputPlugin;
            }
            catch (Exception e) 
            {
                ThrowError($"Failed to instantiate {data} because of an error: {e}");
                return false;
            }

            try
            {
                openConfigDialog = AccessTools.DeclaredMethod(mainType, "OpenConfigDialog", Array.Empty<Type>());
            }
            catch (Exception e)
            {
                LogFile.WriteLine($"Unable to find OpenConfigDialog() in {data} due to an error: {e}");
                openConfigDialog = null;
            }
            return true;
        }

        public void OpenConfig()
        {
            if (plugin == null || openConfigDialog == null)
                return;

            try
            {
                openConfigDialog.Invoke(plugin, Array.Empty<object>());
            }
            catch (Exception e)
            {
                ThrowError($"Failed to open plugin config for {data} because of an error: {e}");
            }
        }

        public bool Init(object gameInstance)
        {
            if (plugin == null)
                return false;

            try
            {

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
                        LogFile.WriteLine($"Registered {count} session components from: {mainAssembly.FullName}");
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

            plugin.Update();
            return true;
        }

        public bool HandleInput()
        {
            if (plugin == null)
                return false;

            inputPlugin?.HandleInput();
            return true;
        }

        public void Dispose()
        {
            if(plugin != null)
            {
                try
                {
                    plugin.Dispose();
                    plugin = null;
                    inputPlugin = null;
                }
                catch (Exception e)
                {
                    data.Status = PluginStatus.Error;
                    LogFile.WriteLine($"Failed to dispose {data} because of an error: {e}");
                }
            }
        }

        private void ThrowError(string error)
        {
            LogFile.WriteLine(error);
            data.Error();
            Dispose();
        }

        public static bool TryGet(PluginData data, out PluginInstance instance)
        {
            instance = null;
            if (data.Status == PluginStatus.Error || !data.TryLoadAssembly(out Assembly a))
                return false;

            Type pluginType = a.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t));
            if (pluginType == null)
            {
                LogFile.WriteLine($"Failed to load {data} because it does not contain an IPlugin");
                data.Error();
                return false;
            }

            instance = new PluginInstance(data, a, pluginType);
            return true;
        }

        public override string ToString()
        {
            return data.ToString();
        }

    }
}