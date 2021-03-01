using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using avaness.PluginLoader.Data;
using Sandbox.Engine.Networking;
using VRage.GameServices;
using VRage.Input;
using VRage;

namespace avaness.PluginLoader
{
    public class PluginConfig
    {
        private const string fileName = "config.xml";

        private SortedDictionary<string, PluginData> plugins = new SortedDictionary<string, PluginData>();
        private string filePath, mainDirectory;
        private LogFile log;

        [XmlIgnore]
        public IReadOnlyDictionary<string, PluginData> Data => plugins;

        [XmlArray]
        [XmlArrayItem(typeof(LocalPlugin))]
        [XmlArrayItem(typeof(WorkshopPlugin))]
        [XmlArrayItem(typeof(SEPMPlugin))]
        public PluginData[] Plugins
        {
            get
            {
                return plugins.Values.ToArray();
            }
            set
            {
                plugins = new SortedDictionary<string, PluginData>(value.ToDictionary(p => p.Id));
            }
        }

        public PluginConfig()
        {

        }

        public bool Load(PluginData data)
        {
            PluginData existing;
            if (plugins.TryGetValue(data.Id, out existing))
            {
                return existing.Enabled;
            }
            else
            {
                plugins[data.Id] = data;
                return data.Enabled;
            }
        }

        public void Init(string filePath, string mainDirectory, LogFile log)
        {
            this.filePath = filePath;
            this.mainDirectory = mainDirectory;
            this.log = log;

            HashSet<string> installed = GetPlugins();

            // Remove plugins from config that no longer exist
            foreach (string id in plugins.Keys.ToArray())
            {
                if (!installed.Remove(id))
                {
                    log.WriteLine($"{id} is no longer available.");
                    plugins.Remove(id);
                }
            }

            Save();
        }

        public void Disable()
        {
            foreach (PluginData data in plugins.Values)
                data.Enabled = false;
        }


        private HashSet<string> GetPlugins()
        {
            log.WriteLine("Finding installed plugins...");
            HashSet<string> installed = new HashSet<string>();

            // Find local plugins
            foreach (string dll in Directory.EnumerateFiles(mainDirectory, "*.dll", SearchOption.AllDirectories))
            {
                LocalPlugin local = new LocalPlugin(dll);
                if (!local.FriendlyName.StartsWith("0Harmony"))
                {
                    installed.Add(local.Id);
                    if (!plugins.ContainsKey(local.Id))
                        plugins.Add(local.Id, local);
                }
            }

            string workshop = Path.GetFullPath(@"..\..\..\workshop\content\244850\");

            // Find workshop plugins
            foreach (string mod in Directory.EnumerateDirectories(workshop))
            {
                try
                {
                    string folder = Path.GetFileName(mod);
                    if (ulong.TryParse(folder, out ulong modId))
                    {
                        if(TryGetPlugin(modId, mod, out PluginData newPlugin))
                        {
                            installed.Add(newPlugin.Id);
                            if (plugins.TryGetValue(newPlugin.Id, out PluginData temp) && temp != null)
                                newPlugin.CopyFrom(temp);

                            plugins[newPlugin.Id] = newPlugin;
                        }
                    }
                    else
                    {
                        log.WriteLine($"Failed to parse {folder} into a steam id.");
                    }
                }
                catch (Exception e)
                {
                    log.WriteLine($"An error occurred while searching {mod} for a plugin: {e}");
                }
            }


            log.WriteLine($"Found {installed.Count} plugins.");
            return installed;
        }

        public void CheckForNewMods(HashSet<ulong> modIds)
        {
            string workshop = Path.GetFullPath(@"..\..\..\workshop\content\244850\");
            foreach (ulong id in modIds)
            {
                if(!plugins.ContainsKey(id.ToString()))
                {
                    string modRoot = Path.Combine(workshop, id.ToString());
                    if (Directory.Exists(modRoot))
                    {
                        try
                        {
                            if (TryGetPlugin(id, modRoot, out PluginData plugin))
                            {
                                log.WriteLine(plugin + " was just installed.");
                                plugins.Add(plugin.Id, plugin);
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        private bool TryGetPlugin(ulong id, string modRoot, out PluginData plugin)
        {
            plugin = null;

            foreach (string file in Directory.EnumerateFiles(modRoot, "*.plugin"))
            {
                string name = Path.GetFileName(file);
                if (!name.StartsWith("0Harmony", StringComparison.OrdinalIgnoreCase))
                {
                    plugin = new WorkshopPlugin(id, file);
                    return true;
                }
            }

            string sepm = Path.Combine(modRoot, "Data", "sepm-plugin.zip");
            if (File.Exists(sepm))
            {
                plugin = new SEPMPlugin(log, id, sepm);
                return true;
            }

            return false;
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

        public static PluginConfig Load(string mainDirectory, LogFile log)
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
                    config.Init(path, mainDirectory, log);
                    return config;
                }
                catch (Exception e)
                {
                    log.WriteLine($"An error occurred while loading plugin config: " + e);
                }
            }


            var temp = new PluginConfig();
            temp.Init(path, mainDirectory, log);
            return temp;
        }
    }
}
