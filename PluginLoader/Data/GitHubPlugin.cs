using avaness.PluginLoader.Compiler;
using avaness.PluginLoader.Network;
using ProtoBuf;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using LitJson;

namespace avaness.PluginLoader.Data
{
    [ProtoContract]
    public class GitHubPlugin : PluginData
    {
        public override string Source => "GitHub";

        [ProtoMember(1)]
        public string Commit { get; set; }

        [ProtoMember(2)]
        [XmlArray]
        [XmlArrayItem("Directory")]
        public string[] SourceDirectories { get; set; }

        private const string pluginFile = "plugin.dll";
        private const string commitHashFile = "commit.sha1";
        private string cacheDir, assemblyName;

        public GitHubPlugin()
        {
            Status = PluginStatus.None;
        }

        public void Init(string mainDirectory)
        {
            string[] nameArgs = Id.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameArgs.Length != 2)
                throw new Exception("Invalid GitHub name: " + Id);

            if(SourceDirectories != null)
            {
                for (int i = SourceDirectories.Length - 1; i >= 0; i--)
                {
                    string path = SourceDirectories[i].Replace('\\', '/').TrimStart('/');
                    
                    if(path.Length == 0)
                    {
                        SourceDirectories.RemoveAtFast(i);
                        continue;
                    }

                    if (path[path.Length - 1] != '/')
                        path += '/';

                    SourceDirectories[i] = path;
                }
            }

            assemblyName = MakeSafeString(nameArgs[1]);
            cacheDir = Path.Combine(mainDirectory, "GitHub", nameArgs[0], nameArgs[1]);
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
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            var lbl = Main.Instance.Splash;

            Assembly a;

            string dllFile = Path.Combine(cacheDir, pluginFile);
            string commitFile = Path.Combine(cacheDir, commitHashFile);
            if (File.Exists(dllFile) && File.Exists(commitFile) && File.ReadAllText(commitFile) == Commit)
            {
                lbl.SetText($"Loading '{FriendlyName}'");
                a = Assembly.LoadFile(dllFile);
                Version = a.GetName().Version;
                return a;
            }

            lbl.SetText($"Downloading '{FriendlyName}'");
            byte[] data = CompileFromSource(x => lbl.SetBarValue(x));
            File.WriteAllBytes(dllFile, data);
            File.WriteAllText(commitFile, Commit);
            Status = PluginStatus.Updated;

            lbl.SetText($"Compiled '{FriendlyName}'");
            a = Assembly.Load(data);
            Version = a.GetName().Version;

            CountDownload();

            return a;
        }

        private void CountDownload()
        {
            var version = Version.ToString();
            try
            {
                GitHub.DownloadRelease(Id, version, "README.md").Close();
                LogFile.WriteLine($"Plugin {Id} download counted for release {version}");
            }
            catch (WebException)
            {
                LogFile.WriteLine($"Plugin {Id} is missing release {version}, download not counted");
            }
            catch (Exception e)
            {
                LogFile.WriteLine($"Failed to count download of plugin {Id}: {e}");
            }
        }

        public byte[] CompileFromSource(Action<float> callback = null)
        {
            RoslynCompiler compiler = new RoslynCompiler();
            using(Stream s = GitHub.DownloadRepo(Id, Commit, out string fileName))
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
            return compiler.Compile(assemblyName + '_' + Path.GetRandomFileName());
        }

        private void CompileFromSource(RoslynCompiler compiler, ZipArchiveEntry entry)
        {
            if (AllowedZipPath(entry.FullName))
            {
                using (Stream entryStream = entry.Open())
                {
                    compiler.Load(entryStream, entry.FullName);
                }
            }
        }

        private bool AllowedZipPath(string path)
        {
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                return false;

            if (SourceDirectories == null || SourceDirectories.Length == 0)
                return true;

            path = RemoveRoot(path); // Make the base of the path the root of the repository

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

        public void UpdateUsage()
        {
            try
            {
                var version = Version.ToString();
                var stream = GitHub.DownloadReposApi(Id, "releases");
                var reader = new StreamReader(stream);
                var json = JsonMapper.ToObject(reader);
                foreach (var release in json)
                {
                    if (!(release is JsonData r))
                        continue;

                    if (r["name"].ToString() != version && r["tag_name"].ToString() != version)
                        continue;

                    foreach (var asset in r["assets"])
                    {
                        if (asset is JsonData a && a["name"].ToString() == "README.md")
                        {
                            Usage = int.Parse(a["download_count"].ToString());
                            return;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogFile.WriteLine($"Failed to retrieve GitHub release statistics for plugin {Id}: {e}");
            }
        }
    }
}