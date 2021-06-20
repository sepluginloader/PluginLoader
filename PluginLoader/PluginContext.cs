using System.IO;
using VRage.Game.ModAPI;

namespace avaness.PluginLoader
{
    public class PluginContext : IMyModContext
    {
        public PluginContext(string name, string id, string path, string service = "Steam")
        {
            ModName = name;
            ModId = id;
            if (Directory.Exists(path))
            {
                ModPath = path;
                string data = Path.Combine(path, "Data");
                if (Directory.Exists(data))
                    ModPathData = data;
            }
            ModServiceName = service;
        }

        public string ModName { get; }
        public string ModId { get; }
        public string ModPath { get; }
        public string ModPathData { get; }
        public string ModServiceName { get; }

        public bool IsBaseGame => false;
    }
}
