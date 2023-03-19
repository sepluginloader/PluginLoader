using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Text;
using VRage.Game;
using avaness.PluginLoader.Data;

namespace avaness.PluginLoader.Config
{
    public class PluginConfig
    {
        private const string fileName = "config.xml";

        private string filePath;
        private PluginList list;

        [XmlArray]
        [XmlArrayItem("Id")]
        public string[] Plugins
        {
            get { return enabledPlugins.Keys.ToArray(); }
            set
            {
                enabledPlugins.Clear();
                foreach (string id in value)
                    enabledPlugins[id] = null;
            }
        }
        public IEnumerable<PluginData> EnabledPlugins => enabledPlugins.Values;
        private readonly Dictionary<string, PluginData> enabledPlugins = new Dictionary<string, PluginData>();

        [XmlArray]
        [XmlArrayItem("Plugin")]
        public Data.LocalFolderPlugin.Config[] LocalFolderPlugins
        {
            get { return PluginFolders.Values.ToArray(); }
            set { PluginFolders = value.ToDictionary(x => x.Folder); }
        }

        [XmlIgnore] public Dictionary<string, Data.LocalFolderPlugin.Config> PluginFolders { get; private set; } = new();

        [XmlArray]
        [XmlArrayItem("Profile")]
        public Profile[] Profiles
        {
            get { return ProfileMap.Values.ToArray(); }
            set
            {
                ProfileMap.Clear();
                foreach (var profile in value)
                    ProfileMap[profile.Key] = profile;
            }
        }

        [XmlIgnore]
        public readonly Dictionary<string, Profile> ProfileMap = new();

        public string ListHash { get; set; }

        public int GameVersion { get; set; }
        [XmlIgnore]
        public bool GameVersionChanged { get; private set; }

        // Base URL for the statistics server, change to http://localhost:5000 in config.xml for local development
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public string StatsServerBaseUrl { get; }

        // User consent to use the StatsServer
        public bool DataHandlingConsent { get; set; }
        public string DataHandlingConsentDate { get; set; }

        private int networkTimeout = 5000;
        public int NetworkTimeout
        {
            get
            {
                return networkTimeout;
            }
            set
            {
                if (value < 100)
                    networkTimeout = 100;
                else if (value > 60000)
                    networkTimeout = 60000;
                else
                    networkTimeout = value;
            }
        }

        public int Count => enabledPlugins.Count;

        public bool AllowIPv6 { get; set; } = true;

        public PluginConfig()
        {
        }

        public void Init(PluginList plugins)
        {
            list = plugins;

            bool save = false;

            // Remove plugins from config that no longer exist
            StringBuilder sb = new StringBuilder("Enabled plugins: ");
            foreach (string id in enabledPlugins.Keys.ToArray())
            {
                if (plugins.TryGetPlugin(id, out PluginData plugin))
                {
                    enabledPlugins[id] = plugin;
                    sb.Append(id).Append(", ");
                }
                else
                {
                    LogFile.WriteLine($"{id} was in the config but is no longer available");
                    enabledPlugins.Remove(id);
                    save = true;
                }
            }

            if (enabledPlugins.Count > 0)
                sb.Length -= 2;
            else
                sb.Append("None");
            LogFile.WriteLine(sb.ToString());

            if (save)
                Save();
        }

        public void CheckGameVersion()
        {
            int currentGameVersion = MyFinalBuildConstants.APP_VERSION?.Version ?? 0;
            int storedGameVersion = GameVersion;
            if (currentGameVersion != 0)
            {
                if (storedGameVersion == 0)
                {
                    GameVersion = currentGameVersion;
                    Save();
                }
                else if (storedGameVersion != currentGameVersion)
                {
                    GameVersion = currentGameVersion;
                    GameVersionChanged = true;
                    Save();
                }
            }
        }

        public void Disable()
        {
            enabledPlugins.Clear();
        }


        public void Save()
        {
            try
            {
                LogFile.WriteLine("Saving config");
                XmlSerializer serializer = new XmlSerializer(typeof(PluginConfig));
                if (File.Exists(filePath))
                    File.Delete(filePath);
                FileStream fs = File.OpenWrite(filePath);
                serializer.Serialize(fs, this);
                fs.Flush();
                fs.Close();
            }
            catch (Exception e)
            {
                LogFile.WriteLine($"An error occurred while saving plugin config: " + e);
            }
        }

        public static PluginConfig Load(string mainDirectory)
        {
            string path = Path.Combine(mainDirectory, fileName);
            if (File.Exists(path))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(PluginConfig));
                    FileStream fs = File.OpenRead(path);
                    PluginConfig config = (PluginConfig)serializer.Deserialize(fs);
                    fs.Close();
                    config.filePath = path;
                    return config;
                }
                catch (Exception e)
                {
                    LogFile.WriteLine($"An error occurred while loading plugin config: " + e);
                }
            }

            return new PluginConfig
            {
                filePath = path
            };
        }

        public bool IsEnabled(string id)
        {
            return enabledPlugins.ContainsKey(id);
        }

        public void SetEnabled(string id, bool enabled)
        {
            if (IsEnabled(id) == enabled)
                return;

            if (enabled)
                Enable(list[id]);
            else
                Disable(id);
        }

        public void SetEnabled(PluginData plugin, bool enabled)
        {
            string id = plugin.Id;
            if (IsEnabled(id) == enabled)
                return;

            if (enabled)
                Enable(plugin);
            else
                Disable(id);
        }

        private void Enable(PluginData plugin)
        {
            string id = plugin.Id;
            enabledPlugins[id] = plugin;
            list.SubscribeToItem(id);
        }

        private void Disable(string id)
        {
            enabledPlugins.Remove(id);
        }

    }
}