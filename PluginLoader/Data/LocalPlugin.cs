using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;

namespace avaness.PluginLoader.Data
{
    public class LocalPlugin : PluginData
    {
        public override string Source => MyTexts.GetString(MyCommonTexts.Local);
        public override string FriendlyName => name;
        private string name;

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
                    name = Path.GetFileName(value);
                else
                    name = "Unknown";
            }
        }

        private LocalPlugin()
        {

        }

        public LocalPlugin(LogFile log, string fullPath) : base(log, fullPath)
        { }

        public override string GetDllFile()
        {
            if(File.Exists(Id))
                return Id;
            return null;
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
