using avaness.PluginLoader.Data;
using Sandbox;
using Sandbox.Game.World;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using VRage.Plugins;

namespace avaness.PluginLoader.Session
{
    public class SessionPlugin
    {
        public Assembly PluginAssembly { get; }

        private readonly LogFile log;
        private readonly PluginData data;
        private readonly IPlugin plugin;
        private readonly PluginStatus status;

        public SessionPlugin(LogFile log, PluginData data, Assembly assembly, IPlugin plugin)
        {
            this.log = log;
            this.data = data;
            status = data.Status;
            data.Status = PluginStatus.Session;
            PluginAssembly = assembly;
            this.plugin = plugin;
            Init();
        }

        private void Init()
        {
            if (plugin is SEPluginManager.SEPMPlugin sepm)
                LoaderTools.ExecuteMain(log, sepm);

            try
            {
                plugin.Init(MySandboxGame.Static);
            }
            catch (Exception e)
            {
                data.Status = PluginStatus.Error;
                log.WriteLine($"An error occurred during Init of {plugin.GetType()}:\n{e}");
            }
        }

        public static bool TryGet(LogFile log, PluginData data, out SessionPlugin plugin)
        {
            plugin = null;

            string dll = data.GetDllFile();
            if (dll == null)
            {
                log.WriteLine("Failed to load " + Path.GetFileName(dll));
                return false;
            }

            Assembly a = Assembly.LoadFile(dll);

            Type pluginType = a.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t));
            if (pluginType == null)
            {
                log.WriteLine($"Failed to load {Path.GetFileName(dll)} because it does not contain an IPlugin.");
                return false;
            }

            plugin = new SessionPlugin(log, data, a, (IPlugin)Activator.CreateInstance(pluginType));
            return true;
        }

        public void Register()
        {
            MySession.Static.RegisterComponentsFromAssembly(PluginAssembly, true);
        }

        public void Update()
        {
            try
            {
                plugin.Update();
            }
            catch (Exception e)
            {
                data.Status = PluginStatus.Error;
                log.WriteLine($"An error occurred during Update of {plugin.GetType()}:\n{e}");
            }
        }

        public void Unload()
        {
            data.Status = status;
            try
            {
                plugin.Dispose();
            }
            catch (Exception e)
            {
                data.Status = PluginStatus.Error;
                log.WriteLine($"An error occurred during Dispose of {plugin.GetType()}:\n{e}");
            }
        }
    }
}
