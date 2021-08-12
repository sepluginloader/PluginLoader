using avaness.PluginLoader.Compiler;
using avaness.PluginLoader.Network;
using ProtoBuf;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using VRage.Game.ModAPI;

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

            if (SourceDirectories != null)
            {
                for (int i = SourceDirectories.Length - 1; i >= 0; i--)
                {
                    string path = SourceDirectories[i].Replace('\\', '/').TrimStart('/');

                    if (path.Length == 0)
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

            Assembly a;

            string dllFile = Path.Combine(cacheDir, pluginFile);
            string commitFile = Path.Combine(cacheDir, commitHashFile);
            if (!File.Exists(dllFile) || !File.Exists(commitFile) || File.ReadAllText(commitFile) != Commit)
            {
                var lbl = Main.Instance.Splash;
                lbl.SetText("Downloading " + this);
                byte[] data = CompileFromSource();
                File.WriteAllBytes(dllFile, data);
                File.WriteAllText(commitFile, Commit);
                Status = PluginStatus.Updated;
                lbl.SetText($"Compiled {this}.");
                a = Assembly.Load(data);
            }
            else
            {
                a = Assembly.LoadFile(dllFile);
            }

            Version = a.GetName().Version;
            return a;
        }



        public byte[] CompileFromSource()
        {
            RoslynCompiler compiler = new RoslynCompiler();
            using (Stream s = GitHub.DownloadRepo(Id, Commit, out string fileName))
            using (ZipArchive zip = new ZipArchive(s))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                    CompileFromSource(compiler, entry);
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

            foreach (string dir in SourceDirectories)
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
            if (index >= 0 && (index + 1) < path.Length)
                return path.Substring(index + 1);
            return path;
        }

        public override void Show()
        {
            MyGuiSandbox.OpenUrl("https://github.com/" + Id, UrlOpenMode.SteamOrExternalWithConfirm);
        }
    }
}