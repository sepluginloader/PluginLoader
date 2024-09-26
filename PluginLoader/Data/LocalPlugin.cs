using Sandbox.Graphics.GUI;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using VRage;

namespace avaness.PluginLoader.Data
{
    public class LocalPlugin : PluginData
    {
        public override string Source => MyTexts.GetString(MyCommonTexts.Local);
        public override bool IsLocal => true;
        public override bool IsCompiled => false;

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

        private AssemblyResolver resolver;

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
                resolver = new AssemblyResolver();
                resolver.AddSourceFolder(Path.GetDirectoryName(Id));
                resolver.AddAllowedAssemblyFile(Id);
                resolver.AssemblyResolved += AssemblyResolved;
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

        private void AssemblyResolved(string assemblyPath)
        {
            Main main = Main.Instance;
            if (!main.Config.IsEnabled(assemblyPath))
                main.List.Remove(assemblyPath);
        }

        public override void GetDescriptionText(MyGuiControlMultilineText textbox)
        {
            textbox.Visible = false;
            textbox.Clear();
        }
    }
}
