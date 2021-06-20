using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using VRage;
using VRage.Game.ModAPI;

namespace avaness.PluginLoader.Data
{
    public class LocalPlugin : PluginData
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
                if (File.Exists(value))
                    FriendlyName = Path.GetFileName(value);
            }
        }


        private LocalPlugin()
        {

        }

        public LocalPlugin(string dll)
        {
            Id = dll;
            Status = PluginStatus.None;
        }

        public override Assembly GetAssembly()
        {
            if(File.Exists(Id))
            {
                AppDomain.CurrentDomain.AssemblyResolve += LoadFromSameFolder;
                Assembly a = Assembly.LoadFile(Id);
                Version = a.GetName().Version;
                return a;
            }
            return null;
        }

        public override string ToString()
        {
            return Id;
        }

        public override void Show()
        {
            string file = Path.GetFullPath(Id);
            if (File.Exists(file))
                Process.Start("explorer.exe", $"/select, \"{file}\"");
        }

        private Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            if (args.RequestingAssembly.IsDynamic)
                return null;

            if (args.Name.Contains("0Harmony") || args.Name.Contains("SEPluginManager"))
                return null;

            string location = args.RequestingAssembly.Location;
            if (string.IsNullOrWhiteSpace(location) || !Path.GetFullPath(location).StartsWith(Path.GetDirectoryName(Id), StringComparison.OrdinalIgnoreCase))
                return null;

            string folderPath = Path.GetDirectoryName(location);
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath))
                return null;

            Assembly assembly = Assembly.LoadFile(assemblyPath);
            LogFile.WriteLine("Resolving " + assembly.GetName().Name + " for " + args.RequestingAssembly.FullName);

            Main main = Main.Instance;
            if (!main.Config.IsEnabled(assemblyPath))
                main.List.Remove(assemblyPath);

            return assembly;
        }

        public override IMyModContext GetContext()
        {
            return new PluginContext(FriendlyName, Path.GetFileName(Id), Path.GetDirectoryName(Id), "LocalPlugin");
        }
    }
}
