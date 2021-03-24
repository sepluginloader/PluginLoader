using ProtoBuf;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace avaness.PluginLoader.Data
{
    [ProtoContract]
    public class SEPMPlugin : SteamPlugin
    {
        private const string NameFile = "name.txt";

        public override string Source => "SEPM";
        protected override string HashFile => "sepm-plugin.txt";

        private string dataFolder;

        protected SEPMPlugin()
        {

        }

        public SEPMPlugin(LogFile log, ulong id, string zipFile) : base(log, id, zipFile)
        { }

        protected override void CheckForUpdates()
        {
            dataFolder = Path.Combine(root, "sepm-plugin");

            if (Directory.Exists(dataFolder))
                base.CheckForUpdates();
            else
                Status = PluginStatus.PendingUpdate;
        }

        protected override void ApplyUpdate()
        {
            if (Directory.Exists(dataFolder))
                Directory.Delete(dataFolder, true);

            ZipFile.ExtractToDirectory(sourceFile, dataFolder);
        }

        protected override string GetAssemblyFile()
        {
            if (!Directory.Exists(dataFolder))
                return null;
            return Directory.EnumerateFiles(dataFolder, "*.dll").Where(s => !s.Equals("0Harmony.dll", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        protected override string GetName()
        {
            if (Status == PluginStatus.PendingUpdate)
            {
                using (ZipArchive archive = ZipFile.OpenRead(sourceFile))
                {
                    ZipArchiveEntry nameEntry = archive.Entries.First(e => e.FullName == NameFile);
                    if (nameEntry != null)
                    {
                        string temp = new StreamReader(nameEntry.Open()).ReadToEnd();
                        if (temp.Length != 0)
                            return temp;
                    }
                }
            }
            else
            {
                string nameFile = Path.Combine(dataFolder, NameFile);
                if (File.Exists(nameFile))
                    return File.ReadAllText(nameFile);
            }

            return Id;
        }
    }
}
