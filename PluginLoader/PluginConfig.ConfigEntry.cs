namespace avaness.PluginLoader
{
    public partial class PluginConfig
    {
        public class ConfigEntry
        {
            public string Id { get; set; }

            private ConfigEntry()
            { }

            public ConfigEntry(string id)
            {
                Id = id;
            }
        }
    }
}
