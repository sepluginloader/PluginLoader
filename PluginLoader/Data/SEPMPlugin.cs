using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace avaness.PluginLoader.Data
{
    public class SEPMPlugin : SteamPlugin
    {
        private const string HashFile = "sepm-plugin.txt";
        private const string NameFile = "name.txt";

        public override string Source => "SEPM";
        public override string FriendlyName => name;

        private string name;
        private bool extracted;
        private readonly string zipFile;
        private readonly LogFile log;

        protected SEPMPlugin()
        {

        }

        public SEPMPlugin(LogFile log, ulong id, string zipFile) : base(id)
        {
            this.log = log;
            this.zipFile = zipFile;
            ExamineZip();
        }

        private void ExamineZip()
        {
            log.WriteLine($"Examining sepm zip file of {Id}");
            string folder = Path.GetDirectoryName(zipFile);
            string hashFile = Path.Combine(folder, HashFile);
            string dataFolder = GetDataFolder(folder);

            // Is data already extracted?
            if (Directory.Exists(dataFolder))
            {
                if (File.Exists(hashFile))
                {
                    string hash = LoaderTools.GetHash(zipFile);
                    if (File.ReadAllText(hashFile) == hash)
                    {
                        extracted = true;
                    }
                    else
                    {
                        Directory.Delete(dataFolder);
                    }
                }
                else
                {
                    Directory.Delete(dataFolder);
                }
            }

            // Find the name of the plugin
            if (extracted)
            {
                string nameFile = Path.Combine(dataFolder, NameFile);
                if (File.Exists(nameFile))
                    name = File.ReadAllText(nameFile);
                Status = PluginStatus.None;
            }
            else
            {
                log.WriteLine($"{Id} requires an update.");
                Status = PluginStatus.PendingUpdate;
                using (ZipArchive archive = ZipFile.OpenRead(zipFile))
                {
                    ZipArchiveEntry nameEntry = archive.Entries.First(e => e.FullName == NameFile);
                    if (nameEntry != null)
                    {
                        string temp = new StreamReader(nameEntry.Open()).ReadToEnd();
                        if (temp.Length != 0)
                            name = temp;
                    }
                }
            }
        }

        private string GetDataFolder(string parent)
        {
            return Path.Combine(parent, Path.GetFileNameWithoutExtension(zipFile).ToLowerInvariant());
        }

        public override string GetDllFile()
        {
            if (zipFile == null)
                return null;

            string folder = Path.GetDirectoryName(zipFile);
            string dataFolder = GetDataFolder(folder);

            if (!extracted)
            {
                log?.WriteLine($"Updating {Id}");
                string hashFile = Path.Combine(folder, HashFile);
                File.WriteAllText(hashFile, LoaderTools.GetHash(zipFile));
                ZipFile.ExtractToDirectory(zipFile, dataFolder);
                extracted = true;
                Status = PluginStatus.Updated;
            }

            return Directory.EnumerateFiles(dataFolder, "*.dll").Where(s => !s.EndsWith("0Harmony.dll", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }
    }
}
