using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace avaness.PluginLoader.Data
{
    public partial class GitHubPlugin
    {
        public class CacheManifest
        {
            private const string pluginFile = "plugin.dll";
            private const string manifestFile = "manifest.xml";
            private const string commitFile = "commit.sha1";
            private const string assetFolder = "Assets";
            private const string libFolder = "Bin";

            private string cacheDir;
            private string assetDir;
            private string libDir;
            private Dictionary<string, AssetFile> assetFiles = new Dictionary<string, AssetFile>();

            [XmlIgnore]
            public string DllFile { get; private set; }
            public string AssetFolder => assetDir;
            public string LibDir => libDir;

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
                    assetFiles = value.ToDictionary(GetAssetKey);
                }
            }

            public CacheManifest()
            {

            }

            private void Init(string cacheDir)
            {
                this.cacheDir = cacheDir;
                assetDir = Path.Combine(cacheDir, assetFolder);
                libDir = Path.Combine(cacheDir, libFolder);
                DllFile = Path.Combine(cacheDir, pluginFile);

                foreach (AssetFile file in assetFiles.Values)
                    SetBaseDir(file);

                // Backwards compatibility
                string oldCommitFile = Path.Combine(cacheDir, commitFile);
                if(File.Exists(oldCommitFile))
                {
                    try
                    {
                        Commit = File.ReadAllText(oldCommitFile).Trim();
                        File.Delete(oldCommitFile);
                        Save();
                    }
                    catch (Exception e)
                    {
                        LogFile.WriteLine("Error while reading old commit file: " + e);
                    }
                }
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

            public bool IsCacheValid(string currentCommit, int currentGameVersion, bool requiresAssets, bool requiresPackages)
            {
                if(!File.Exists(DllFile) || Commit != currentCommit)
                    return false;

                if (GameVersion != 0 && currentGameVersion != 0 && GameVersion != currentGameVersion)
                    return false;

                if (requiresAssets && !assetFiles.Values.Any(x => x.Type == AssetFile.AssetType.Asset))
                    return false;

                if (requiresPackages && !assetFiles.Values.Any(x => x.Type != AssetFile.AssetType.Asset))
                    return false;

                foreach (AssetFile file in assetFiles.Values)
                {
                    if (!file.IsValid())
                        return false;
                }

                return true;
            }

            public void ClearAssets()
            {
                assetFiles.Clear();
            }

            public AssetFile CreateAsset(string file, AssetFile.AssetType type = AssetFile.AssetType.Asset)
            {
                file = file.Replace('\\', '/').TrimStart('/');
                AssetFile asset = new AssetFile(file, type);
                SetBaseDir(asset);
                asset.GetFileInfo();
                assetFiles[GetAssetKey(asset)] = asset;
                return asset;
            }

            private string GetAssetKey(AssetFile asset)
            {
                if (asset.Type == AssetFile.AssetType.Asset)
                    return assetFolder + "/" + asset.NormalizedFileName;
                return libFolder + "/" + asset.NormalizedFileName;
            }

            private void SetBaseDir(AssetFile asset)
            {
                asset.BaseDir = asset.Type == AssetFile.AssetType.Asset ? assetDir : libDir;
            }

            public bool IsAssetValid(AssetFile asset)
            {
                return asset.IsValid();
            }

            public void SaveAsset(AssetFile asset, Stream stream)
            {
                asset.Save(stream);
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
                DeleteUnknownFiles(assetDir);
                DeleteUnknownFiles(libDir);
            }

            public void DeleteUnknownFiles(string assetDir)
            {
                if (!Directory.Exists(assetDir))
                    return;

                foreach(string file in Directory.EnumerateFiles(assetDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = file.Substring(cacheDir.Length).Replace('\\', '/').TrimStart('/');
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