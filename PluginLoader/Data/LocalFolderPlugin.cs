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
        public override string Source => MyTexts.GetString(MyCommonTexts.Local);

        public override string Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
                xmlDialogFolder = Id;
                if (Directory.Exists(value))
                    FriendlyName = Path.GetFileName(value);
            }
        }

        private string xmlDialogFolder;


        private LocalFolderPlugin()
        {

        }

        public LocalFolderPlugin(string folder)
        {
            Id = folder;
            Status = PluginStatus.None;
        }

        public override Assembly GetAssembly()
        {
            if (Directory.Exists(Id))
            {
                RoslynCompiler compiler = new RoslynCompiler();
                bool hasFile = false;
                StringBuilder sb = new StringBuilder();
                sb.Append("Compiling files from ").Append(Id).Append(":").AppendLine();
                foreach(var file in GetProjectFiles(Id))
                {
                    using (FileStream fileStream = File.OpenRead(file))
                    {
                        hasFile = true;
                        sb.Append(file, Id.Length, file.Length - Id.Length).Append(", ");
                        compiler.Load(fileStream, Path.GetFileName(file));
                    }
                }

                if(hasFile)
                {
                    sb.Length -= 2;
                    LogFile.WriteLine(sb.ToString());
                }
                else
                    return null;

                byte[] data = compiler.Compile(FriendlyName + '_' + Path.GetRandomFileName());
                Assembly a = Assembly.Load(data);
                Version = a.GetName().Version;
                return a;
            }
            return null;
        }

        private IEnumerable<string> GetProjectFiles(string folder)
        {
            string gitOutput = null;
            try
            {
                Process p = new Process();

                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = "git";
                p.StartInfo.Arguments = "ls-files --cached --others --exclude-standard";
                p.StartInfo.WorkingDirectory = folder;
                p.Start();

                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // Read the output stream first and then wait.
                gitOutput = p.StandardOutput.ReadToEnd();
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    string[] files = gitOutput.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    return files.Where(x => x.EndsWith(".cs")).Select(x => Path.Combine(folder, x.Trim().Replace('/', Path.DirectorySeparatorChar)));
                }
                else
                {
                    StringBuilder sb = new StringBuilder("An error occurred while checking git for project files.");
                    if (!string.IsNullOrWhiteSpace(gitOutput))
                    {
                        sb.AppendLine("Git output: ");
                        sb.Append(gitOutput).AppendLine();
                    }
                    LogFile.WriteLine(sb.ToString());
                }
            }
            catch (Exception e) 
            {
                StringBuilder sb = new StringBuilder("An error occurred while checking git for project files.");
                if(!string.IsNullOrWhiteSpace(gitOutput))
                {
                    sb.AppendLine("Git output: ");
                    sb.Append(gitOutput).AppendLine();
                }
                sb.AppendLine("Exception: ");
                sb.Append(e);
                LogFile.WriteLine(sb.ToString());
            }


            char sep = Path.DirectorySeparatorChar;
            return Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
                .Where(x => !x.Contains(sep + "bin" + sep) && !x.Contains(sep + "obj" + sep));
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
            menu.AddItem(new StringBuilder("Load data file"));
            return true;
        }

        public override void ContextMenuClicked(MyGuiScreenPluginConfig screen, MyGuiControlContextMenu.EventArgs args)
        {
            Main main = Main.Instance;
            switch (args.ItemIndex)
            {
                case 0:
                    screen.EnablePlugin(this, false);
                    main.Config.PluginFolders.Remove(Id);
                    break;
                case 1:
                    Thread t = new Thread(new ThreadStart(() => OpenDialog(screen)));
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                    break;
            }
        }

        // Open a dialog in a new thread
        private void OpenDialog(MyGuiScreenPluginConfig screen)
        {
            try
            {
                // Get the file path via prompt
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = xmlDialogFolder;
                    openFileDialog.Filter = "Xml files (*.xml)|*.xml|All files (*.*)|*.*";
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog(LoaderTools.GetMainForm()) == DialogResult.OK)
                    {
                        // Move back to the main thread so that we can interact with keen code again
                        MySandboxGame.Static.Invoke(
                            () => DeserializeFile(screen, openFileDialog.FileName),
                            "PluginLoader");
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Error while opening file dialog: " + e);
            }
        }

        // Deserializes a file and refreshes the plugin screen
        private void DeserializeFile(MyGuiScreenPluginConfig screen, string file)
        {
            if (!File.Exists(file))
                return;

            try
            {
                XmlSerializer xml = new XmlSerializer(typeof(PluginData));

                using (StreamReader reader = File.OpenText(file))
                {
                    object resultObj = xml.Deserialize(reader);
                    if(resultObj.GetType() != typeof(GitHubPlugin))
                    {
                        throw new Exception("Xml file is not of type GitHubPlugin!");
                    }

                    GitHubPlugin github = (GitHubPlugin)resultObj;
                    FriendlyName = github.FriendlyName;
                    Tooltip = github.Tooltip;
                    Author = github.Author;
                    Description = github.Description;
                    xmlDialogFolder = Path.GetDirectoryName(file);
                    if(screen.Visible && screen.IsOpened)
                        screen.RefreshSidePanel();
                }
            }
            catch (Exception e)
            {
                LogFile.WriteLine("Error while reading the xml file: " + e);
            }
        }
    }
}
