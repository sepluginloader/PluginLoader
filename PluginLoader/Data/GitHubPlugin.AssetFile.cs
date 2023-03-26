using System.IO;
using System.Xml.Serialization;

namespace avaness.PluginLoader.Data
{
    public partial class GitHubPlugin
    {
        public class AssetFile
        {
            public string Name { get; set; }
            public string Hash { get; set; }
            public long Length { get; set; }

            public string NormalizedFileName => Name.Replace('\\', '/').TrimStart('/');

            public AssetFile()
            {

            }

            public AssetFile(string file)
            {
                Name = file;
            }

            public void GetFileInfo(string assetFolder)
            {
                string file = Path.Combine(assetFolder, Name);
                if (!File.Exists(file))
                    return;

                FileInfo info = new FileInfo(file);
                Length = info.Length;
                Hash = LoaderTools.GetHash256(file);
            }

            public bool IsValid(string assetFolder)
            {
                string file = Path.Combine(assetFolder, Name);
                if (!File.Exists(file))
                    return false;

                FileInfo info = new FileInfo(file);
                if (info.Length != Length)
                    return false;

                string newHash = LoaderTools.GetHash256(file);
                if (newHash != Hash)
                    return false;

                return true;
            }

            public void Save(Stream stream, string assetFolder)
            {
                string newFile = Path.Combine(assetFolder, Name);
                Directory.CreateDirectory(Path.GetDirectoryName(newFile));
                using (FileStream file = File.Create(newFile))
                {
                    stream.CopyTo(file);
                }

                GetFileInfo(assetFolder);
            }

        }
    }
}