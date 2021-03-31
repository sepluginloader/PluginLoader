using avaness.PluginLoader.Compiler;
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
using System.Xml.Linq;

namespace avaness.PluginLoader.Data
{
    [ProtoContract]
    public class GitHubPlugin : PluginData
    {
        public override string Source => "GitHub";

        [ProtoMember(1)]
        public string Commit { get; set; }
        [ProtoMember(2)]
        public string ProjectFile { get; set; }

        private const string pluginFile = "plugin.dll";
        private const string commitHashFile = "commit.sha1";
        private string cacheDir;

        public GitHubPlugin()
        {
            Status = PluginStatus.None;
        }

        public void Init(string mainDirectory)
        {
            string[] nameArgs = Id.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameArgs.Length != 2)
                throw new Exception("Invalid GitHub name: " + Id);

            cacheDir = Path.Combine(mainDirectory, "GitHub", nameArgs[0], nameArgs[1]);
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
                var lbl = Main.Instance.Label;
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
            using(Stream s = GitHub.DownloadRepo(Id, Commit, out string fileName))
            using (ZipArchive zip = new ZipArchive(s))
            {
                if (fileName != null && ProjectFile != null)
                {
                    string root = Path.GetFileNameWithoutExtension(fileName);
                    CompileProject(compiler, zip, Path.Combine(root, ProjectFile.Replace('\\', '/')).Replace('\\', '/'));
                }
                else
                {
                    CompileAllFiles(compiler, zip);
                }
            }
            return compiler.Compile();
        }

        private void CompileProject(RoslynCompiler compiler, ZipArchive zip, string csprojPath)
        {
            ZipArchiveEntry csprojEntry = zip.GetEntry(csprojPath);
            if (csprojEntry == null)
                throw new NullReferenceException(csprojPath + " Does not exist!");

            using(Stream csprojStream = csprojEntry.Open())
            {
                // Source: https://stackoverflow.com/a/28694200
                XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
                XDocument projDefinition = XDocument.Load(csprojStream);

                // References
                IEnumerable<string> references = projDefinition
                    .Element(msbuild + "Project")
                    .Elements(msbuild + "ItemGroup")
                    .Elements(msbuild + "Reference")
                    .Attributes("Include")
                    .Select(refElem => refElem.Value);
                foreach (string reference in references)
                    RoslynReferences.LoadReference(reference);

                string pathRoot = Path.GetDirectoryName(csprojEntry.FullName.Replace('\\', '/')).Replace('\\', '/');

                // Files
                IEnumerable<string> csFiles = projDefinition
                    .Element(msbuild + "Project")
                    .Elements(msbuild + "ItemGroup")
                    .Elements(msbuild + "Compile")
                    .Attributes("Include")
                    .Select(refElem => GetRelativePath(pathRoot, refElem.Value));
                foreach(string csFile in csFiles)
                {
                    if(csFile.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        ZipArchiveEntry entry = zip.GetEntry(csFile);
                        if(entry != null)
                        {
                            using (Stream s = entry.Open())
                            {
                                compiler.Load(s, entry.FullName);
                            }
                        }
                    }
                }
            }
        }

        private string GetRelativePath(string root, string path)
        {
            char[] seps = new char[] { '/', '\\' };

            Stack<string> s = new Stack<string>(root.Split(seps, StringSplitOptions.RemoveEmptyEntries));
            foreach(string p in path.Split(seps, StringSplitOptions.RemoveEmptyEntries))
            {
                if (p == "..")
                    s.Pop();
                else
                    s.Push(p);
            }

            StringBuilder sb = new StringBuilder();
            foreach (string p in s.Reverse())
            {
                if (sb.Length > 0)
                    sb.Append('/');
                sb.Append(p);
            }
            return sb.ToString();
        }

        private void CompileAllFiles(RoslynCompiler compiler, ZipArchive zip)
        {
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                if (entry.FullName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    using (Stream entryStream = entry.Open())
                    {
                        compiler.Load(entryStream, entry.FullName);
                    }
                }

            }
        }

        public override void Show()
        {
            MyGuiSandbox.OpenUrl("https://github.com/" + Id, UrlOpenMode.SteamOrExternalWithConfirm);
        }
    }
}
