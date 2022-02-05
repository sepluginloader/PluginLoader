using avaness.PluginLoader.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ProtoBuf;
using System.Linq;
using avaness.PluginLoader.Network;
using System.IO.Compression;

namespace avaness.PluginLoader
{
    public class PluginList : IEnumerable<PluginData>
    {
        private readonly Dictionary<string, PluginData> plugins = new Dictionary<string, PluginData>();

        public int Count => plugins.Count;

        public PluginData this[string key]
        {
            get => plugins[key];
            set => plugins[key] = value;
        }

        public bool Contains(string id) => plugins.ContainsKey(id);
        public bool TryGetPlugin(string id, out PluginData pluginData) => plugins.TryGetValue(id, out pluginData);

        public PluginList(string mainDirectory, PluginConfig config)
        {
            var lbl = Main.Instance.Splash;

            lbl.SetText("Downloading plugin list...");
            DownloadList(mainDirectory, config);

            FindWorkshopPlugins(config);
            FindLocalPlugins(mainDirectory);
            LogFile.WriteLine($"Found {plugins.Count} plugins");
            FindPluginGroups();
            FindModDependencies();
        }

        /// <summary>
        /// Ensures the user is subscribed to the steam plugin.
        /// </summary>
        public void SubscribeToItem(string id)
        {
            if(plugins.TryGetValue(id, out PluginData data) && data is ISteamItem steam)
                SteamAPI.SubscribeToItem(steam.WorkshopId);
        }

        public bool Remove(string id)
        {
            return plugins.Remove(id);
        }

        private void FindPluginGroups()
        {
            int groups = 0;
            foreach (var group in plugins.Values.Where(x => !string.IsNullOrWhiteSpace(x.GroupId)).GroupBy(x => x.GroupId))
            {
                groups++;
                foreach (PluginData data in group)
                    data.Group.AddRange(group.Where(x => x != data));
            }
            if (groups > 0)
                LogFile.WriteLine($"Found {groups} plugin groups");
        }

        private void FindModDependencies()
        {
            foreach(PluginData data in plugins.Values)
            {
                if (data is ModPlugin mod)
                    FindModDependencies(mod);
            }
        }

        private void FindModDependencies(ModPlugin mod)
        {
            if (mod.DependencyIds == null)
                return;

            Dictionary<ulong, ModPlugin> dependencies = new Dictionary<ulong, ModPlugin>();
            dependencies.Add(mod.WorkshopId, mod);
            Stack<ModPlugin> toProcess = new Stack<ModPlugin>();
            toProcess.Push(mod);

            while (toProcess.Count > 0)
            {
                ModPlugin temp = toProcess.Pop();

                if (temp.DependencyIds == null)
                    continue;

                foreach (ulong id in temp.DependencyIds)
                {
                    if (!dependencies.ContainsKey(id) && plugins.TryGetValue(id.ToString(), out PluginData data) && data is ModPlugin dependency)
                    {
                        toProcess.Push(dependency);
                        dependencies[id] = dependency;
                    }
                }
            }

            dependencies.Remove(mod.WorkshopId);
            mod.Dependencies = dependencies.Values.ToArray();
        }

        private void DownloadList(string mainDirectory, PluginConfig config)
        {
            string whitelist = Path.Combine(mainDirectory, "whitelist.bin");

            try
            {
                LogFile.WriteLine("Downloading whitelist");
                if (!File.Exists(whitelist) | ListChanged(config.ListHash, out string hash))
                {
                    using (Stream zipFileStream = GitHub.DownloadRepo(GitHub.listRepoName, GitHub.listRepoCommit, out _))
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
                                try
                                {
                                    PluginData data = (PluginData)xml.Deserialize(entryReader);
                                    plugins[data.Id] = data;
                                }
                                catch (InvalidOperationException e)
                                {
                                    LogFile.WriteLine("An error occurred while reading the plugin xml: " + (e.InnerException ?? e));
                                }
                            }
                        }
                    }
                    
                    LogFile.WriteLine("Saving whitelist to disk");
                    using (Stream binFile = File.Create(whitelist))
                    {
                        Serializer.Serialize(binFile, plugins.Values.ToArray());
                    }
                    LogFile.WriteLine("Whitelist updated");

                    config.ListHash = hash;
                    config.Save();
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
                    LogFile.WriteLine("Reading whitelist from cache");
                    using (Stream binFile = File.OpenRead(whitelist))
                    {
                        foreach (PluginData data in Serializer.Deserialize<PluginData[]>(binFile))
                            plugins[data.Id] = data;
                    }
                    LogFile.WriteLine("Whitelist retrieved from disk");
                    return;
                }
                catch (Exception e)
                {
                    LogFile.WriteLine("Error while reading whitelist: " + e);
                }
            }
                
            LogFile.WriteLine("No whitelist is available! Plugin list will contain local plugins only.");
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

        public bool Exists(string id)
        {
            return plugins.TryGetValue(id, out PluginData data);
        }

        private void FindLocalPlugins(string mainDirectory)
        {
            foreach (string dll in Directory.EnumerateFiles(mainDirectory, "*.dll", SearchOption.AllDirectories))
            {
                if(!dll.Contains(Path.DirectorySeparatorChar + "GitHub" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    LocalPlugin local = new LocalPlugin(dll);
                    string name = local.FriendlyName;
                    if (!name.StartsWith("0Harmony") && !name.StartsWith("Microsoft"))
                        plugins[local.Id] = local;
                }
            }
        }

        private void FindWorkshopPlugins(PluginConfig config)
        {
            List<ISteamItem> steamPlugins = new List<ISteamItem>(plugins.Values.Select(x => x as ISteamItem).Where(x => x != null));

            Main.Instance.Splash.SetText($"Updating workshop items...");

            SteamAPI.Update(steamPlugins.Where(x => config.IsEnabled(x.Id)).Select(x => x.WorkshopId));

            string workshop = Path.GetFullPath(@"..\..\..\workshop\content\244850\");
            foreach (ISteamItem steam in steamPlugins)
            {
                try
                {
                    string path = Path.Combine(workshop, steam.Id);
                    if(Directory.Exists(path))
                    {
                        if (steam is SteamPlugin plugin && TryGetPlugin(path, out string dllFile))
                            plugin.Init(dllFile);
                    }
                    else if (config.IsEnabled(steam.Id))
                    {
                        ((PluginData)steam).Status = PluginStatus.Error;
                        LogFile.WriteLine($"The plugin '{steam}' is missing and cannot be loaded.");
                    }
                }
                catch (Exception e)
                {
                    LogFile.WriteLine($"An error occurred while searching for the workshop plugin {steam}: {e}");
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