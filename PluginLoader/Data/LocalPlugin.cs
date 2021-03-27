using System.Diagnostics;
using System.IO;
using System.Reflection;
using VRage;

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
                return Assembly.LoadFile(Id);
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
    }
}
