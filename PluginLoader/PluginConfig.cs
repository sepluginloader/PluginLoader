using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using avaness.PluginLoader.Data;
using Sandbox.Engine.Networking;
using VRage.Game;

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
            get
            {
                return EnabledPlugins.ToArray();
            }
            set
            {
                EnabledPlugins = new HashSet<string>(value);
            }
        }

        [XmlIgnore]
        public HashSet<string> EnabledPlugins { get; private set; } = new HashSet<string>();

        public string ListHash { get; set; }

        // Base URL for the statistics server, change to http://localhost:5000 in config.xml for local development
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public string StatsServerBaseUrl { get; }

        // User consent to use the StatsServer
        public bool DataHandlingConsent { get; set; }
        public string DataHandlingConsentDate { get; set; }

        public int Count => EnabledPlugins.Count;

        public PluginConfig()
        {

        }

        public void Init(PluginList plugins)
        {
            // Remove plugins from config that no longer exist
            List<string> toRemove = new List<string>();

            foreach (string id in EnabledPlugins)
            {
                if (!plugins.Exists(id))
                {
                    LogFile.WriteLine($"{id} is no longer available");
                    toRemove.Add(id);
                }
            }

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
                EnableDependencies(id);
                Main.Instance.List.SubscribeToItem(id);
            }
            else
            {
                EnabledPlugins.Remove(id);
            }
        }

        private void EnableDependencies(string id)
        {
            if (Main.Instance.List[id] is not ModPlugin plugin)
                return;

            var idHashSet = new HashSet<WorkshopId> { new(plugin.WorkshopId, "Steam") };
            var dependencyHierarchy = MyWorkshop.GetModsDependencyHiearchy(idHashSet, out var hasReferenceIssue);

            // Return value cannot be null based on current SE code.
            // But just in case the code would change:
            if (dependencyHierarchy == null)
            {
                LogFile.WriteLine($"WARNING: Got null hierarchy while getting dependencies of mod {id}");
                return;
            }

            if (hasReferenceIssue)
            {
                LogFile.WriteLine($"WARNING: Reference issue detected while getting dependencies of mod {id}");
                return;
            }

            foreach (var workshopItem in dependencyHierarchy)
            {
                var pluginId = workshopItem.Id.ToString();
                if (Main.Instance.List.Contains(pluginId))
                    SetEnabled(pluginId, true);
                else
                    SteamAPI.SubscribeToItem(workshopItem.Id);
            }
        }
    }
}