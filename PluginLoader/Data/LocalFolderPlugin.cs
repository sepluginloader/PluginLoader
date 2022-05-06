using avaness.PluginLoader.Compiler;
using avaness.PluginLoader.GUI;
using Sandbox;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using VRage;
using VRage.Utils;

namespace avaness.PluginLoader.Data
{
    public class LocalFolderPlugin : PluginData
    {
        const string XmlDataType = "Xml files (*.xml)|*.xml|All files (*.*)|*.*";
        const int GitTimeout = 10000;

        public override string Source => MyTexts.GetString(MyCommonTexts.Local);

        public Config FolderSettings { get; }
        public GitHubPlugin DataFile { get; private set;  }

        public LocalFolderPlugin(Config settings, GitHubPlugin github = null)
        {
            Id = settings.Folder;
            FriendlyName = Path.GetFileName(Id);
            Status = PluginStatus.None;
            FolderSettings = settings;
            if(github == null)
                github = GitHubPlugin.DeserializeFile(settings.DataFile);
            DataFile = github;
            CopyData(github);
        }

        public override Assembly GetAssembly()
        {
            if (Directory.Exists(Id))
            {
                RoslynCompiler compiler = new RoslynCompiler(FolderSettings.DebugBuild);
                bool hasFile = false;
                StringBuilder sb = new StringBuilder();
                sb.Append("Compiling files from ").Append(Id).Append(":").AppendLine();
                foreach(var file in GetProjectFiles(Id, DataFile.SourceDirectories))
                {
                    using (FileStream fileStream = File.OpenRead(file))
                    {
                        hasFile = true;
                        string name = file.Substring(Id.Length + 1, file.Length - (Id.Length + 1));
                        sb.Append(name).Append(", ");
                        compiler.Load(fileStream, file);
                    }
                }

                if(hasFile)
                {
                    sb.Length -= 2;
                    LogFile.WriteLine(sb.ToString());
                }
                else
                {
                    throw new IOException("No files were found in the directory specified.");
                }

                byte[] data = compiler.Compile(FriendlyName + '_' + Path.GetRandomFileName(), out byte[] symbols);
                Assembly a = Assembly.Load(data, symbols);
                Version = a.GetName().Version;
                return a;
            }

            throw new DirectoryNotFoundException("Unable to find directory '" + Id + "'");
        }

        public static IEnumerable<string> GetProjectFiles(string folder, string[] sourceDirectories)
        {
            string gitError = null;
            try
            {
                Process p = new Process();

                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.FileName = "git";
                p.StartInfo.Arguments = "ls-files --cached --others --exclude-standard";
                p.StartInfo.WorkingDirectory = folder;
                p.Start();

                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // Read the output stream first and then wait.
                string gitOutput = p.StandardOutput.ReadToEnd();
                gitError = p.StandardError.ReadToEnd();
                if(!p.WaitForExit(GitTimeout))
                {
                    p.Kill();
                    throw new TimeoutException("Git operation timed out.");
                }

                if (p.ExitCode == 0)
                {
                    string[] files = gitOutput.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    return files.Where(x => x.EndsWith(".cs") && IsValidProjectFile(x, sourceDirectories)).Select(x => Path.Combine(folder, x.Trim().Replace('/', Path.DirectorySeparatorChar))).Where(x => File.Exists(x));
                }
                else
                {
                    StringBuilder sb = new StringBuilder("An error occurred while checking git for project files. " + p.HasExited);
                    if (!string.IsNullOrWhiteSpace(gitError))
                    {
                        sb.AppendLine(" Git output: ");
                        sb.Append(gitError).AppendLine();
                    }
                    LogFile.WriteLine(sb.ToString());
                }
            }
            catch (Exception e) 
            {
                StringBuilder sb = new StringBuilder("An error occurred while checking git for project files.");
                if(!string.IsNullOrWhiteSpace(gitError))
                {
                    sb.AppendLine(" Git output: ");
                    sb.Append(gitError).AppendLine();
                }
                sb.AppendLine("Exception: ");
                sb.Append(e);
                LogFile.WriteLine(sb.ToString());
            }


            char sep = Path.DirectorySeparatorChar;
            return Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
                .Where(x => !x.Contains(sep + "bin" + sep) && !x.Contains(sep + "obj" + sep) && IsValidProjectFile(x, sourceDirectories));
        }

        private static bool IsValidProjectFile(string file, string[] sourceDirectories)
        {
            if (sourceDirectories == null || sourceDirectories.Length == 0)
                return true;
            file = file.Replace('\\', '/');
            foreach(string dir in sourceDirectories)
            {
                if (file.StartsWith(dir))
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            return Id;
        }

        public override void Show()
        {
            string folder = Path.GetFullPath(Id);
            if (Directory.Exists(folder))
                Process.Start("explorer.exe", $"\"{folder}\"");
        }

        public override bool OpenContextMenu(MyGuiControlContextMenu menu)
        {
            menu.Clear();
            menu.AddItem(new StringBuilder("Remove"));
            menu.AddItem(new StringBuilder("Edit"));
            if(FolderSettings.DebugBuild)
                menu.AddItem(new StringBuilder("Switch to release build"));
            else
                menu.AddItem(new StringBuilder("Switch to debug build"));
            return true;
        }

        public override void ContextMenuClicked(MyGuiScreenPluginConfig screen, MyGuiControlContextMenu.EventArgs args)
        {
            switch (args.ItemIndex)
            {
                case 0:
                    Main.Instance.Config.PluginFolders.Remove(Id);
                    screen.RemovePlugin(this);
                    screen.RequireRestart();
                    break;
                case 1:
                    MyGuiSandbox.AddScreen(new MyGuiScreenLocalFolder(screen, this));
                    break;
                case 2:
                    FolderSettings.DebugBuild = !FolderSettings.DebugBuild;
                    screen.RequireRestart();
                    break;

            }
        }

        // Deserializes a file and refreshes the plugin screen
        public void CopyData(GitHubPlugin github)
        {
            FriendlyName = github.FriendlyName;
            Tooltip = github.Tooltip;
            Author = github.Author;
            Description = github.Description;
            DataFile = github;
        }


        public class Config
        {
            public Config() { }

            public Config(string folder, string dataFile)
            {
                Folder = folder;
                DataFile = dataFile;
            }

            public Config Copy()
            {
                Config copy = new Config(Folder, DataFile)
                {
                    DebugBuild = DebugBuild
                };
                return copy;
            }

            public string Folder { get; set; }
            public string DataFile { get; set; }
            public bool DebugBuild { get; set; } = true;
            public bool Valid => Directory.Exists(Folder) && File.Exists(DataFile);
        }
    }
}
