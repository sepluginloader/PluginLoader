using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Text;

namespace avaness.PluginLoader
{
    public class PluginConfig
    {
        private const string fileName = "config.xml";

        private string filePath;

        [XmlArray]
        [XmlArrayItem("Id")]
        public string[] Plugins
        {
            get { return EnabledPlugins.ToArray(); }
            set { EnabledPlugins = new HashSet<string>(value); }
        }

        [XmlIgnore] public HashSet<string> EnabledPlugins { get; private set; } = new();

        [XmlArray]
        [XmlArrayItem("Folder")]
        public string[] LocalFolderPlugins
        {
            get { return PluginFolders.ToArray(); }
            set { PluginFolders = new HashSet<string>(value); }
        }

        [XmlIgnore] public HashSet<string> PluginFolders { get; private set; } = new();

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

        public int Count => EnabledPlugins.Count;

        public PluginConfig()
        {
        }

        public void Init(PluginList plugins)
        {
            // Remove plugins from config that no longer exist
            List<string> toRemove = new List<string>();

            StringBuilder sb = new StringBuilder("Enabled plugins: ");
            foreach (string id in EnabledPlugins)
            {
                if (!plugins.Contains(id))
                {
                    LogFile.WriteLine($"{id} was in the config but is no longer available");
                    toRemove.Add(id);
                }
                else
                {
                    sb.Append(id).Append(", ");
                }
            }

            if (EnabledPlugins.Count > 0)
                sb.Length -= 2;
            else
                sb.Append("None");
            LogFile.WriteLine(sb.ToString());


            foreach (string id in toRemove)
                EnabledPlugins.Remove(id);

            if (toRemove.Count > 0)
                Save();
        }

        public void Disable()
        {
            EnabledPlugins.Clear();
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

        public IEnumerator<string> GetEnumerator()
        {
            return EnabledPlugins.GetEnumerator();
        }

        public bool IsEnabled(string id)
        {
            return EnabledPlugins.Contains(id);
        }

        public void SetEnabled(string id, bool enabled)
        {
            if (EnabledPlugins.Contains(id) == enabled)
                return;

            if (enabled)
            {
                EnabledPlugins.Add(id);
                Main.Instance.List.SubscribeToItem(id);
            }
            else
            {
                EnabledPlugins.Remove(id);
            }
        }
    }
}