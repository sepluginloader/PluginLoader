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
        private readonly LogFile log;
        private readonly SortedDictionary<string, PluginData> plugins = new SortedDictionary<string, PluginData>();
        
        public PluginData this[string key]
        {
            get => plugins[key];
            set => plugins[key] = value;
        }

        public PluginList(string mainDirectory, PluginConfig config, LogFile log)
        {
            this.log = log;

            DownloadList(mainDirectory, config);

            log.WriteLine("Finding installed plugins...");
            FindWorkshopPlugins();
            FindLocalPlugins(mainDirectory);
            log.WriteLine($"Found {plugins.Count} plugins.");
        }

        private void DownloadList(string mainDirectory, PluginConfig config)
        {
            string whitelist = Path.Combine(mainDirectory, "whitelist.bin");

            try
            {
                log.WriteLine("Downloading whitelist...");
                if (!File.Exists(whitelist) | ListChanged(config.ListHash, out string hash))
                {
                    config.ListHash = hash;

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
                    
                    log.WriteLine("Saving whitelist to disk...");
                    using (Stream binFile = File.Create(whitelist))
                    {
                        Serializer.Serialize(binFile, plugins.Values.ToArray());
                    }
                    log.WriteLine("Whitelist updated.");
                    return;
                }
            }
            catch (Exception e)
            {
                log.WriteLine("Error while downloading whitelist: " + e);
            }

            if (File.Exists(whitelist))
            {
                try
                {
                    log.WriteLine("Reading whitelist...");
                    using (Stream binFile = File.OpenRead(whitelist))
                    {
                        foreach (PluginData data in Serializer.Deserialize<PluginData[]>(binFile))
                            plugins[data.Id] = data;
                    }
                    log.WriteLine("Whitelist retrieved from disk.");
                }
                catch (Exception e)
                {
                    log.WriteLine("Error while reading whitelist: " + e);
                }
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

        public bool Exists(string id)
        {
            return plugins.ContainsKey(id);
        }

        private void FindLocalPlugins(string mainDirectory)
        {
            foreach (string dll in Directory.EnumerateFiles(mainDirectory, "*.dll", SearchOption.AllDirectories))
            {
                LocalPlugin local = new LocalPlugin(log, dll);
                if (!local.FriendlyName.StartsWith("0Harmony"))
                    plugins[local.Id] = local;
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
                    if (ulong.TryParse(folder, out ulong modId) && SteamAPI.IsSubscribed(modId) && TryGetPlugin(modId, mod, out PluginData newPlugin))
                    {
                        if (plugins.ContainsKey(newPlugin.Id))
                            plugins[newPlugin.Id] = newPlugin;
                        else
                            log.WriteLine($"The item {newPlugin} is not on the plugin list.");
                    }
                }
                catch (Exception e)
                {
                    log.WriteLine($"An error occurred while searching {mod} for a plugin: {e}");
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
                    plugin = new WorkshopPlugin(log, id, file);
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



        public IEnumerator<PluginData> GetEnumerator()
        {
            return plugins.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return plugins.Values.GetEnumerator();
        }



        private readonly static HashSet<ulong> whitelistItemIds = new HashSet<ulong>
        {
            // Workshop
            2292390607, // Tool Switcher
            2413859055, // SteamWorkshopFix
            2413918072, // SEWorldGenPlugin v2
            2414532651, // DecalFixPlugin
            2415983416, // Multigrid Projector
            2425805190, // MorePPSettings
            2004495632, // BlockPicker
            1937528740, // GridFilter
            2029854486, // RemovePlanetSizeLimits
            2171994463, // ClientFixes
            2156683844, // SEWorldGenPlugin
            1937530079, // Mass Rename
            2037606896, // CameraLCD
            2432659774, // ScrollableFOV
        };

        private readonly static HashSet<string> whitelistItemSha = new HashSet<string>()
        {
            "fa6d204bcb706bb5ba841e06e19b2793f324d591093e67ca0752d681fb5e6352", // Jump Selector Plugin
            "275afaead0e5a7d83b0c5be5f13fe67b8b96e375dba4fd5cf4976f6dbce58e81"
        };

        public static bool Validate(ulong steamId, string file, out string sha256)
        {
            sha256 = null;
            if (whitelistItemIds.Contains(steamId))
                return true;
            sha256 = LoaderTools.GetHash256(file);
            return whitelistItemSha.Contains(sha256);
        }
    }
}
