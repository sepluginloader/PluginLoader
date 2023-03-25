using avaness.PluginLoader.Compiler;
using avaness.PluginLoader.Config;
using avaness.PluginLoader.GUI;
using avaness.PluginLoader.Network;
using ProtoBuf;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace avaness.PluginLoader.Data
{
    [ProtoContract]
    public class GitHubPlugin : PluginData
    {
        public override string Source => "GitHub";
        public override bool IsLocal => false;

        [ProtoMember(1)]
        public string Commit { get; set; }

        [ProtoMember(2)]
        [XmlArray]
        [XmlArrayItem("Directory")]
        public string[] SourceDirectories { get; set; }

        [ProtoMember(3)]
        [XmlArray]
        [XmlArrayItem("Version")]
        public Branch[] AlternateVersions { get; set; }

        [ProtoMember(4)]
        [XmlArray]
        [XmlArrayItem("Directory")]
        public string[] AssetFolders { get; set; }

        [ProtoMember(5)]
        [XmlArray]
        [XmlArrayItem("Package")]
        public NuGetPackage[] NuGetReferences { get; set; }

        private const string pluginFile = "plugin.dll";
        private const string commitHashFile = "commit.sha1";
        private string cacheDir, assemblyName;
        private GitHubPluginConfig config;

        public GitHubPlugin()
        {
            Status = PluginStatus.None;
        }

        public override bool LoadData(ref PluginDataConfig config, bool enabled)
        {
            if (enabled)
            {
                if (config is GitHubPluginConfig githubConfig && IsValidConfig(githubConfig))
                {
                    this.config = githubConfig;
                    return false;
                }

                this.config = new GitHubPluginConfig()
                {
                    Id = Id,
                };
                config = this.config;
                return true;
            }

            this.config = null;

            if (config != null)
            {
                config = null;
                return true;
            }

            return false;
        }

        private bool IsValidConfig(GitHubPluginConfig githubConfig)
        {
            if (string.IsNullOrWhiteSpace(githubConfig.SelectedVersion))
                return true;
            if (AlternateVersions == null)
                return false;
            return AlternateVersions.Any(x => x.Name.Equals(githubConfig.SelectedVersion, StringComparison.OrdinalIgnoreCase));
        }

        public void InitPaths()
        {
            string[] nameArgs = Id.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameArgs.Length < 2)
                throw new Exception("Invalid GitHub name: " + Id);

            CleanPaths(SourceDirectories);
            CleanPaths(AssetFolders);

            assemblyName = MakeSafeString(nameArgs[1]);
            cacheDir = Path.Combine(LoaderTools.PluginsDir, "GitHub", nameArgs[0], nameArgs[1]);
        }

        private void CleanPaths(string[] paths)
        {
            if (paths != null)
            {
                for (int i = paths.Length - 1; i >= 0; i--)
                {
                    string path = paths[i].Replace('\\', '/').TrimStart('/');

                    if (path.Length == 0)
                        continue;

                    if (path[path.Length - 1] != '/')
                        path += '/';

                    paths[i] = path;
                }
            }
        }

        private string MakeSafeString(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in s)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(ch);
                else
                    sb.Append('_');
            }
            return sb.ToString();
        }

        public override Assembly GetAssembly()
        {
            InitPaths();

            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            InstallReferences();

            Assembly a;

            string dllFile = Path.Combine(cacheDir, pluginFile);
            string commitFile = Path.Combine(cacheDir, commitHashFile);
            string selectedCommit = GetSelectedVersion()?.Commit ?? Commit;
            if (!File.Exists(dllFile) || !File.Exists(commitFile) || File.ReadAllText(commitFile) != selectedCommit || Main.Instance.Config.GameVersionChanged || !HasAssetFolders())
            {
                var lbl = Main.Instance.Splash;
                lbl.SetText($"Downloading '{FriendlyName}'");
                byte[] data = CompileFromSource(selectedCommit, x => lbl.SetBarValue(x));
                File.WriteAllBytes(dllFile, data);
                File.WriteAllText(commitFile, selectedCommit);
                Status = PluginStatus.Updated;
                lbl.SetText($"Compiled '{FriendlyName}'");
                a = Assembly.Load(data);
            }
            else
            {
                a = Assembly.LoadFile(dllFile);
            }

            Version = a.GetName().Version;
            return a;
        }

        private void InstallReferences()
        {
            if (NuGetReferences == null || NuGetReferences.Length == 0)
                return;

            Task.Run(InstallReferencesAsync).GetAwaiter().GetResult();
        }

        private async Task InstallReferencesAsync()
        {
            foreach (NuGetPackage reference in NuGetReferences)
                await Main.Instance.NuGet.InstallPackage(reference.Name, reference.Version);
        }

        private bool HasAssetFolders()
        {
            if (AssetFolders == null || AssetFolders.Length == 0)
                return true;
            return AssetFolders.All(x => Directory.Exists(GetAssetFolderPath(x)));
        }

        private string GetAssetFolderPath(string assetFolder)
        {
            return Path.Combine(cacheDir, Path.GetFileName(Path.GetDirectoryName(assetFolder)));
        }

        private Branch GetSelectedVersion()
        {
            if (config == null || string.IsNullOrWhiteSpace(config.SelectedVersion))
                return null;
            return AlternateVersions?.FirstOrDefault(x => x.Name.Equals(config.SelectedVersion, StringComparison.OrdinalIgnoreCase));
        }

        public byte[] CompileFromSource(string commit, Action<float> callback = null)
        {
            RoslynCompiler compiler = new RoslynCompiler();
            using (Stream s = GitHub.DownloadRepo(Id, commit))
            using (ZipArchive zip = new ZipArchive(s))
            {
                callback?.Invoke(0);
                for (int i = 0; i < zip.Entries.Count; i++)
                {
                    ZipArchiveEntry entry = zip.Entries[i];
                    CompileFromSource(compiler, entry);
                    callback?.Invoke(i / (float)zip.Entries.Count);
                }
                callback?.Invoke(1);
            }
            return compiler.Compile(assemblyName + '_' + Path.GetRandomFileName(), out _);
        }

        private void CompileFromSource(RoslynCompiler compiler, ZipArchiveEntry entry)
        {
            string path = RemoveRoot(entry.FullName);
            if (AllowedZipPath(path))
            {
                using (Stream entryStream = entry.Open())
                {
                    compiler.Load(entryStream, entry.FullName);
                }
            }
            if (IsAssetZipPath(path, out string assetFilePath))
            {
                string newFile = Path.Combine(cacheDir, assetFilePath);
                Directory.CreateDirectory(Path.GetDirectoryName(newFile));
                using (Stream entryStream = entry.Open())
                using (FileStream file = File.Create(newFile))
                {
                    entryStream.CopyTo(file);
                }
            }
        }

        private bool IsAssetZipPath(string path, out string assetFilePath)
        {
            assetFilePath = null;

            if (AssetFolders == null || AssetFolders.Length == 0 || path.EndsWith("/"))
                return false;

            foreach (string dir in AssetFolders)
            {
                if (path.StartsWith(dir, StringComparison.Ordinal) && path.Length > (dir.Length + 1))
                {
                    assetFilePath = Path.Combine(GetAssetFolderPath(dir), path.Substring(dir.Length).TrimStart('/'));
                    return true;
                }
            }
            return false;
        }

        private bool AllowedZipPath(string path)
        {
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return false;

            if (SourceDirectories == null || SourceDirectories.Length == 0)
                return true;

            foreach(string dir in SourceDirectories)
            {
                if (path.StartsWith(dir, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private string RemoveRoot(string path)
        {
            path = path.Replace('\\', '/').TrimStart('/');
            int index = path.IndexOf('/');
            if(index >= 0 && (index + 1) < path.Length)
                return path.Substring(index + 1);
            return path;
        }

        public override void Show()
        {
            MyGuiSandbox.OpenUrl("https://github.com/" + Id, UrlOpenMode.SteamOrExternalWithConfirm);
        }

        public override void InvalidateCache()
        {
            try
            {
                string commitFile = Path.Combine(cacheDir, commitHashFile);
                if (File.Exists(commitFile))
                    File.Delete(commitFile);
                LogFile.WriteLine($"Cache for GitHub plugin {Id} was invalidated, it will need to be compiled again at next game start");
            }
            catch (Exception e)
            {
                LogFile.WriteLine("ERROR: Failed to invalidate github cache: " + e);
            }
        }

        public override void AddDetailControls(PluginDetailMenu screen, MyGuiControlBase bottomControl, out MyGuiControlBase topControl)
        {
            if(AlternateVersions == null || AlternateVersions.Length == 0)
            {
                topControl = null;
                return;
            }

            string selectedCommit = GetSelectedVersion()?.Commit ?? Commit;
            MyGuiControlCombobox versionDropdown = new MyGuiControlCombobox();
            versionDropdown.AddItem(-1, "Default");
            int selectedKey = -1;
            for (int i = 0; i < AlternateVersions.Length; i++)
            {
                Branch version = AlternateVersions[i];
                versionDropdown.AddItem(i, version.Name);
                if (version.Commit == selectedCommit)
                    selectedKey = i;
            }
            versionDropdown.SelectItemByKey(selectedKey);
            versionDropdown.ItemSelected += () =>
            {
                PluginConfig mainConfig = Main.Instance.Config;

                int selectedKey = (int)versionDropdown.GetSelectedKey();
                string newVersion = selectedKey >= 0 ? AlternateVersions[selectedKey].Name : null;
                string currentVersion = GetSelectedVersion()?.Name;
                if (currentVersion == newVersion)
                    return;

                if (config == null)
                {
                    config = new GitHubPluginConfig()
                    {
                        Id = Id,
                    };

                    mainConfig.SavePluginData(config);
                }

                config.SelectedVersion = newVersion;
                mainConfig.Save();
                if(mainConfig.IsEnabled(Id))
                    screen.InvokeOnRestartRequired();
            };

            screen.PositionAbove(bottomControl, versionDropdown, MyAlignH.Left);
            screen.Controls.Add(versionDropdown);

            MyGuiControlLabel lblVersion = new MyGuiControlLabel(text: "Installed Version");
            screen.PositionAbove(versionDropdown, lblVersion, align: MyAlignH.Left, spacing: 0);
            screen.Controls.Add(lblVersion);
            topControl = lblVersion;
        }

        [ProtoContract]
        public class Branch
        {
            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public string Commit { get; set; }

        }

        [ProtoContract]
        public class NuGetPackage
        {

            [ProtoMember(1)]
            public string Name { get; set; }

            [ProtoMember(2)]
            public string Version { get; set; }
        }
    }
}