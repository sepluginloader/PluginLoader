using avaness.PluginLoader.Compiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;

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
                if (Directory.Exists(value))
                    FriendlyName = Path.GetFileName(value);
            }
        }


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

        public IEnumerable<string> GetProjectFiles(string folder)
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
    }
}
