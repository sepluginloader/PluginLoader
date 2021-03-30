using avaness.PluginLoader.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ProtoBuf;
using System.Linq;
using avaness.PluginLoader.Network;
using System.Net.Sockets;
using System.IO.Compression;

namespace avaness.PluginLoader
{
    public class PluginList : IEnumerable<PluginData>
    {
        private readonly SortedDictionary<string, PluginData> plugins = new SortedDictionary<string, PluginData>();
        
        public PluginData this[string key]
        {
            get => plugins[key];
            set => plugins[key] = value;
        }

        public PluginList(string mainDirectory, PluginConfig config)
        {
            var lbl = Main.Instance.Label;

            lbl.SetText("Downloading plugin list...");
            DownloadList(mainDirectory, config);

            lbl.SetText("Finding installed plugins...");
            LogFile.WriteLine("Finding installed plugins...");
            FindWorkshopPlugins();
            FindLocalPlugins(mainDirectory);
            LogFile.WriteLine($"Found {plugins.Count} plugins.");
        }

        private void DownloadList(string mainDirectory, PluginConfig config)
        {
            string whitelist = Path.Combine(mainDirectory, "whitelist.bin");

            try
            {
                LogFile.WriteLine("Downloading whitelist...");
                if (!File.Exists(whitelist) | ListChanged(config.ListHash, out string hash))
                {
                    using (Stream zipFileStream = GitHub.DownloadRepo(GitHub.listRepoName, GitHub.listRepoCommit))
                    using (ZipArchive zipFile = new ZipArchive(zipFileStream))
                    {
                        XmlSerializer xml = new XmlSerializer(typeof(PluginData));
                        foreach(var entry in zipFile.Entries)
                        {
                            if (!entry.FullName.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
                                continue;

                            using(Stream entryStream = entry.Open())
                            using(StreamReader entryReader = new StreamReader(entryStream))
                            {
                                PluginData data = (PluginData)xml.Deserialize(entryReader);
                                plugins[data.Id] = data;
                            }
                        }
                    }
                    
                    LogFile.WriteLine("Saving whitelist to disk...");
                    using (Stream binFile = File.Create(whitelist))
                    {
                        Serializer.Serialize(binFile, plugins.Values.ToArray());
                    }
                    LogFile.WriteLine("Whitelist updated.");

                    config.ListHash = hash;
                    return;
                }
            }
            catch (Exception e)
            {
                LogFile.WriteLine("Error while downloading whitelist: " + e);
            }

            if (File.Exists(whitelist))
            {
                try
                {
                    LogFile.WriteLine("Reading whitelist from cache...");
                    using (Stream binFile = File.OpenRead(whitelist))
                    {
                        foreach (PluginData data in Serializer.Deserialize<PluginData[]>(binFile))
                            plugins[data.Id] = data;
                    }
                    LogFile.WriteLine("Whitelist retrieved from disk.");
                }
                catch (Exception e)
                {
                    LogFile.WriteLine("Error while reading whitelist: " + e);
                }
            }
        }

        private static void Save(PluginData data, string path)
        {
            XmlSerializer xml = new XmlSerializer(typeof(PluginData));
            using (Stream file = File.Create(path))
            {
                xml.Serialize(file, data);
            }
        }

        private bool ListChanged(string current, out string hash)
        {
            using (Stream hashStream = GitHub.DownloadFile(GitHub.listRepoName, GitHub.listRepoCommit, GitHub.listRepoHash))
            using (StreamReader hashStreamReader = new StreamReader(hashStream))
            {
                hash = hashStreamReader.ReadToEnd().Trim();
            }

            return current == null || current != hash;
        }

        public bool IsInstalled(string id)
        {
            return plugins.TryGetValue(id, out PluginData data) && data.Status != PluginStatus.NotInstalled;
        }

        private void FindLocalPlugins(string mainDirectory)
        {
            foreach (string dll in Directory.EnumerateFiles(mainDirectory, "*.dll", SearchOption.AllDirectories))
            {
                if(!dll.Contains(Path.DirectorySeparatorChar + "GitHub" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    LocalPlugin local = new LocalPlugin(dll);
                    if (!local.FriendlyName.StartsWith("0Harmony"))
                        plugins[local.Id] = local;
                }
            }
        }

        private void FindWorkshopPlugins()
        {
            string workshop = Path.GetFullPath(@"..\..\..\workshop\content\244850\");

            foreach (string mod in Directory.EnumerateDirectories(workshop))
            {

                try
                {
                    string folder = Path.GetFileName(mod);
                    if (ulong.TryParse(folder, out ulong modId) && SteamAPI.IsSubscribed(modId) && TryGetPlugin(mod, out string newPlugin))
                    {
                        if (plugins.TryGetValue(folder, out PluginData data) && data is SteamPlugin steam)
                            steam.Init(newPlugin);
                        else
                            LogFile.WriteLine($"The item {folder} is not on the plugin list.");
                    }
                }
                catch (Exception e)
                {
                    LogFile.WriteLine($"An error occurred while searching {mod} for a plugin: {e}");
                }
            }
        }

        private bool TryGetPlugin(string modRoot, out string pluginFile)
        {

            foreach (string file in Directory.EnumerateFiles(modRoot, "*.plugin"))
            {
                string name = Path.GetFileName(file);
                if (!name.StartsWith("0Harmony", StringComparison.OrdinalIgnoreCase))
                {
                    pluginFile = file;
                    return true;
                }
            }

            string sepm = Path.Combine(modRoot, "Data", "sepm-plugin.zip");
            if (File.Exists(sepm))
            {
                pluginFile = sepm;
                return true;
            }
            pluginFile = null;
            return false;
        }



        public IEnumerator<PluginData> GetEnumerator()
        {
            return plugins.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return plugins.Values.GetEnumerator();
        }
    }
}
