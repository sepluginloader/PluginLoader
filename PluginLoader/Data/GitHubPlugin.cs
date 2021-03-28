using avaness.PluginLoader.Compiler;
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

        [ProtoMember(1)]
        public string Commit { get; set; }

        private const string pluginFile = "plugin.dll";
        private const string commitHashFile = "commit.sha1";
        private string cacheDir;

        public GitHubPlugin()
        {
            Status = PluginStatus.None;
        }

        public void Init(string mainDirectory)
        {
            string[] nameArgs = Id.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameArgs.Length != 2)
                throw new Exception("Invalid GitHub name: " + Id);

            cacheDir = Path.Combine(mainDirectory, "GitHub", nameArgs[0], nameArgs[1]);
        }

        public override Assembly GetAssembly()
        {
            if (!Directory.Exists(cacheDir))
                Directory.CreateDirectory(cacheDir);

            string dllFile = Path.Combine(cacheDir, pluginFile);
            string commitFile = Path.Combine(cacheDir, commitHashFile);
            if (!File.Exists(dllFile) || !File.Exists(commitFile) || File.ReadAllText(commitFile) != Commit)
            {
                byte[] data = CompileFromSource();
                File.WriteAllBytes(dllFile, data);
                File.WriteAllText(commitFile, Commit);
                Status = PluginStatus.Updated;
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
                RoslynReferences.GenerateAssemblyList();

                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    if(entry.FullName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        using (Stream entryStream = entry.Open())
                        {
                            RoslynReferences.LoadReferences(entryStream);
                        }
                    }
                    else if (entry.FullName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        using (Stream entryStream = entry.Open())
                        {
                            MemoryStream mem = new MemoryStream();
                            using (mem)
                            {
                                entryStream.CopyTo(mem);
                                compiler.Load(new RoslynCompiler.Source(mem, entry.FullName));
                            }
                        }
                    }

                }
            }
            return compiler.Compile();
        }

        public override void Show()
        {
            MyGuiSandbox.OpenUrl("https://github.com/" + Id, UrlOpenMode.SteamOrExternalWithConfirm);
        }
    }
}
