using System;

namespace avaness.PluginLoader
{
    public class Profile
    {
        // Unique key of the profile
        public string Key { get; set; }

        // Name of the profile
        public string Name { get; set; }

        // Plugin IDs
        public string[] Plugins { get; set; }

        public Profile()
        {
        }

        public Profile(string name, string[] plugins)
        {
            Key = Guid.NewGuid().ToString();
            Name = name;
            Plugins = plugins;
        }
    }
}