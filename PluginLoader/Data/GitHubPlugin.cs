using avaness.PluginLoader.Network;
using ProtoBuf;
using Sandbox.Graphics.GUI;
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace avaness.PluginLoader.Data
{
    [ProtoContract]
    public class GitHubPlugin : PluginData
    {
        public override string Source => "GitHub";

        public override string FriendlyName => Name;

        [ProtoMember(1)]
        public string Name { get; set; } = "";
        [ProtoMember(2)]
        public string Commit { get; set; }

        private string cacheDir;

        public GitHubPlugin()
        { }

        public override string GetDllFile()
        {
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            string dllFile = Path.Combine(cacheDir, "plugin.dll");
            string commitFile = Path.Combine(cacheDir, "commit.sha1");
            if (!File.Exists(dllFile) || !File.Exists(commitFile) || File.ReadAllText(commitFile) != Commit)
            {
                File.WriteAllText(commitFile, Commit);
                byte[] data = CompileFromSource();
                File.WriteAllBytes(dllFile, data);
                return Assembly.Load(data);
            }
            return Assembly.LoadFile(dllFile);
        }

        public byte[] CompileFromSource()
        {
            RoslynCompiler compiler = new RoslynCompiler();
            using(Stream s = GitHub.DownloadRepo(Id, Commit))
            using (ZipArchive zip = new ZipArchive(s))
            {
                foreach(ZipArchiveEntry entry in zip.Entries)
                {
                    if (!entry.FullName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        continue;

                    using (Stream entryStream = entry.Open())
                    {
                        compiler.Load(entryStream);
                    }
                }
            }
            return compiler.Compile(log);
        }

        public override void Show()
        {
            MyGuiSandbox.OpenUrl("https://github.com/" + Id, UrlOpenMode.SteamOrExternalWithConfirm);
        }
    }
}
