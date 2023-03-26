using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace avaness.PluginLoader.Data
{
    public partial class GitHubPlugin
    {
        public class CacheManifest
        {
            private const string pluginFile = "plugin.dll";
            private const string manifestFile = "manifest.xml";

            private string cacheDir;
            private string assetDir;
            private Dictionary<string, AssetFile> assetFiles = new Dictionary<string, AssetFile>();

            [XmlIgnore]
            public string DllFile { get; private set; }
            public string AssetFolder => assetDir;

            public string Commit { get; set; }
            public int GameVersion { get; set; }
            [XmlArray]
            [XmlArrayItem("File")]
            public AssetFile[] AssetFiles
            {
                get
                {
                    return assetFiles.Values.ToArray();
                }
                set
                {
                    assetFiles = value.ToDictionary(x => x.NormalizedFileName);
                }
            }

            public CacheManifest()
            {

            }

            private void Init(string cacheDir)
            {
                this.cacheDir = cacheDir;
                assetDir = Path.Combine(cacheDir, "Assets");
                DllFile = Path.Combine(cacheDir, pluginFile);
            }

            public static CacheManifest Load (string userName, string repoName)
            {
                string cacheDir = Path.Combine(LoaderTools.PluginsDir, "GitHub", userName, repoName);
                Directory.CreateDirectory(cacheDir);

                CacheManifest manifest;

                string manifestLocation = Path.Combine(cacheDir, manifestFile);
                if(!File.Exists(manifestLocation))
                {
                    manifest = new CacheManifest();
                }
                else
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(CacheManifest));
                    try
                    {
                        using (Stream file = File.OpenRead(manifestLocation))
                            manifest = (CacheManifest)serializer.Deserialize(file);
                    }
                    catch (Exception e)
                    {
                        LogFile.WriteLine("Error while loading manifest file: " + e);
                        manifest = new CacheManifest();
                    }
                }

                manifest.Init(cacheDir);
                return manifest;
            }

            public bool IsCacheValid(string currentCommit, int currentGameVersion)
            {
                if(!File.Exists(DllFile) || Commit != currentCommit || GameVersion == 0)
                    return false;

                if (currentGameVersion != 0 && GameVersion != currentGameVersion)
                    return false;

                foreach (AssetFile file in assetFiles.Values)
                {
                    if (!file.IsValid(assetDir))
                        return false;
                }

                return true;
            }

            public void ClearAssets()
            {
                assetFiles.Clear();
            }

            public AssetFile CreateAsset(string file)
            {
                file = file.Replace('\\', '/').TrimStart('/');
                AssetFile asset = new AssetFile(file);
                asset.GetFileInfo(assetDir);
                assetFiles[file] = asset;
                return asset;
            }

            public bool IsAssetValid(AssetFile asset)
            {
                return asset.IsValid(assetDir);
            }

            public void SaveAsset(AssetFile asset, Stream stream)
            {
                asset.Save(stream, assetDir);
            }

            public void Save()
            {
                string manifestLocation = Path.Combine(cacheDir, manifestFile);
                XmlSerializer serializer = new XmlSerializer(typeof(CacheManifest));
                try
                {
                    using (Stream file = File.Create(manifestLocation))
                        serializer.Serialize(file, this);
                }
                catch (Exception e)
                {
                    LogFile.WriteLine("Error while saving manifest file: " + e);
                }
            }

            public void DeleteUnknownFiles()
            {
                if (!Directory.Exists(cacheDir))
                    return;

                foreach(string file in Directory.EnumerateFiles(assetDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = file.Substring(assetDir.Length).Replace('\\', '/').TrimStart('/');
                    if (!assetFiles.ContainsKey(relativePath))
                        File.Delete(file);
                }
            }

            public void Invalidate()
            {
                Commit = null;
                Save();
            }
        }
    }
}