using System.Reflection;

namespace avaness.PluginLoader.Data
{
    internal class ObsoletePlugin : PluginData
    {
        public override string Source => "Obsolete";
        public override bool IsLocal => false;

        public override Assembly GetAssembly()
        {
            return null;
        }

        public override void Show()
        {

        }
    }
}