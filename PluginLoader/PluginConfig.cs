using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using avaness.PluginLoader.Data;
using System.Collections;

namespace avaness.PluginLoader
{
    public partial class PluginConfig
    {
        private const string fileName = "config.xml";

        private HashSet<string> enabledPlugins = new HashSet<string>();
        private string filePath;
        private LogFile log;

        [XmlArray]
        public ConfigEntry[] Plugins
        {
            get
            {
                return enabledPlugins.Select(x => new ConfigEntry(x)).ToArray();
            }
            set
            {
                enabledPlugins = new HashSet<string>(value.Select(x => x.Id));
            }
        }

        public int Count => enabledPlugins.Count;

        public PluginConfig()
        {

        }

        public void Init(string filePath, string mainDirectory, PluginList plugins, LogFile log)
        {
            this.filePath = filePath;
            this.log = log;

            // Remove plugins from config that no longer exist
            foreach (string id in enabledPlugins)
            {
                if (!plugins.Exists(id))
                {
                    log.WriteLine($"{id} is no longer available.");
                    enabledPlugins.Remove(id);
                }
            }

            Save();
        }

        public void Disable()
        {
            enabledPlugins.Clear();
        }


        public void Save()
        {
            try
            {
                log.WriteLine("Saving config.");
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
                log.WriteLine($"An error occurred while saving plugin config: " + e);
            }
        }

        public static PluginConfig Load(string mainDirectory, PluginList plugins, LogFile log)
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
                    config.Init(path, mainDirectory, plugins, log);
                    return config;
                }
                catch (Exception e)
                {
                    log.WriteLine($"An error occurred while loading plugin config: " + e);
                }
            }


            var temp = new PluginConfig();
            temp.Init(path, mainDirectory, plugins, log);
            return temp;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return enabledPlugins.GetEnumerator();
        }

        public bool IsEnabled(string id)
        {
            return enabledPlugins.Contains(id);
        }
        
        public void SetEnabled(string id, bool enabled)
        {
            if (enabled)
                enabledPlugins.Add(id);
            else
                enabledPlugins.Remove(id);
        }
    }
}
