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
using avaness.PluginLoader.Config;

namespace avaness.PluginLoader
{
    public class PluginList : IEnumerable<PluginData>
    {
        private Dictionary<string, PluginData> plugins = new Dictionary<string, PluginData>();

        public int Count => plugins.Count;

        public bool HasError { get; private set; }

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

            if(plugins.Count == 0)
            {
                LogFile.WriteLine("WARNING: No plugins in the plugin list. Plugin list will contain local plugins only.");
                HasError = true;
            }

            UpdateWorkshopItems(config);

            FindLocalPlugins(config, mainDirectory);
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

        public void Add(PluginData data)
        {
            plugins[data.Id] = data;
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

            PluginData[] list;
            string currentHash = config.ListHash;
            string newHash;
            if (!TryDownloadWhitelistHash(out newHash))
            {
                // No connection to plugin hub, read from cache
                if (!TryReadWhitelistFile(whitelist, out list))
                    return;
            }
            else if(currentHash == null || currentHash != newHash)
            {
                // Plugin list changed, try downloading new version first
                if (!TryDownloadWhitelistFile(whitelist, newHash, config, out list) 
                    && !TryReadWhitelistFile(whitelist, out list))
                    return;
            }
            else
            {
                // Plugin list did not change, try reading the current version first
                if (!TryReadWhitelistFile(whitelist, out list) 
                    && !TryDownloadWhitelistFile(whitelist, newHash, config, out list))
                    return;
            }

            if(list != null)
                plugins = list.ToDictionary(x => x.Id);
        }

        private bool TryReadWhitelistFile(string file, out PluginData[] list)
        {
            list = null;

            if (File.Exists(file) && new FileInfo(file).Length > 0)
            {
                LogFile.WriteLine("Reading whitelist from cache");
                try
                {
                    PluginData[] rawData;
                    using (Stream binFile = File.OpenRead(file))
                    {
                        rawData = Serializer.Deserialize<PluginData[]>(binFile);
                    }

                    int obsolete = 0;
                    List<PluginData> tempList = new List<PluginData>(rawData.Length);
                    foreach (PluginData data in rawData)
                    {
                        if (data is ObsoletePlugin)
                            obsolete++;
                        else
                            tempList.Add(data);
                    }
                    LogFile.WriteLine("Whitelist retrieved from disk");
                    list = tempList.ToArray();
                    if (obsolete > 0)
                        LogFile.WriteLine("WARNING: " + obsolete + " obsolete plugins found in the whitelist file.");
                    return true;
                }
                catch (Exception e)
                {
                    LogFile.WriteLine("Error while reading whitelist: " + e);
                }
            }
            else
            {
                LogFile.WriteLine("No whitelist cache exists");
            }

            return false;
        }

        private bool TryDownloadWhitelistFile(string file, string hash, PluginConfig config, out PluginData[] list)
        {
            list = null;
            Dictionary<string, PluginData> newPlugins = new Dictionary<string, PluginData>();

            try
            {
                using (Stream zipFileStream = GitHub.DownloadRepo(GitHub.listRepoName, GitHub.listRepoCommit))
                using (ZipArchive zipFile = new ZipArchive(zipFileStream))
                {
                    XmlSerializer xml = new XmlSerializer(typeof(PluginData));
                    foreach (var entry in zipFile.Entries)
                    {
                        if (!entry.FullName.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
                            continue;

                        using (Stream entryStream = entry.Open())
                        using (StreamReader entryReader = new StreamReader(entryStream))
                        {
                            try
                            {
                                PluginData data = (PluginData)xml.Deserialize(entryReader);
                                newPlugins[data.Id] = data;
                            }
                            catch (InvalidOperationException e)
                            {
                                LogFile.WriteLine("An error occurred while reading the plugin xml: " + (e.InnerException ?? e));
                            }
                        }
                    }
                }

                list = newPlugins.Values.ToArray();
                return TrySaveWhitelist(file, list, hash, config);
            }
            catch (Exception e)
            {
                LogFile.WriteLine("Error while downloading whitelist: " + e);
            }

            return false;
        }

        private bool TrySaveWhitelist(string file, PluginData[] list, string hash, PluginConfig config)
        {
            try
            {
                LogFile.WriteLine("Saving whitelist to disk");
                using (MemoryStream mem = new MemoryStream())
                {
                    Serializer.Serialize(mem, list);
                    using (Stream binFile = File.Create(file))
                    {
                        mem.WriteTo(binFile);
                    }
                }

                config.ListHash = hash;
                config.Save();

                LogFile.WriteLine("Whitelist updated");
                return true;
            }
            catch (Exception e)
            {
                LogFile.WriteLine("Error while saving whitelist: " + e);
                try
                {
                    File.Delete(file);
                }
                catch { }
                return false;
            }
        }

        private bool TryDownloadWhitelistHash(out string hash)
        {
            hash = null;
            try
            {
                using (Stream hashStream = GitHub.DownloadFile(GitHub.listRepoName, GitHub.listRepoCommit, GitHub.listRepoHash))
                using (StreamReader hashStreamReader = new StreamReader(hashStream))
                {
                    hash = hashStreamReader.ReadToEnd().Trim();
                }
                return true;
            }
            catch (Exception e)
            {
                LogFile.WriteLine("Error while downloading whitelist hash: " + e);
                return false;
            }
        }

        private void FindLocalPlugins(PluginConfig config, string mainDirectory)
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

            foreach(var folderConfig in config.PluginFolders.Values)
            {
                if(folderConfig.Valid)
                {
                    LocalFolderPlugin local = new LocalFolderPlugin(folderConfig);
                    plugins[local.Id] = local;
                }
            }
        }

        private void UpdateWorkshopItems(PluginConfig config)
        {
            List<ISteamItem> steamPlugins = new List<ISteamItem>(plugins.Values.Select(x => x as ISteamItem).Where(x => x != null));

            Main.Instance.Splash.SetText($"Updating workshop items...");

            SteamAPI.Update(steamPlugins.Where(x => config.IsEnabled(x.Id)).Select(x => x.WorkshopId));
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