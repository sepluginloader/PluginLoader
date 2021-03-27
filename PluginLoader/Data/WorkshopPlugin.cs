using ProtoBuf;
using System.IO;
using VRage;

namespace avaness.PluginLoader.Data
{
    [ProtoContract]
    public class WorkshopPlugin : SteamPlugin
    {
        public override string Source => MyTexts.GetString(MyCommonTexts.Workshop);
        protected override string HashFile => "hash.txt";

        private string assembly;

        protected WorkshopPlugin()
        {

        }

        protected override void CheckForUpdates()
        {
            assembly = Path.Combine(root, Path.GetFileNameWithoutExtension(sourceFile) + ".dll");

            bool found = false;
            foreach (string dll in Directory.EnumerateFiles(root, "*.dll"))
            {
                if (dll == assembly)
                    found = true;
                else
                    File.Delete(dll);
            }
            if (!found)
                Status = PluginStatus.PendingUpdate;
            else
                base.CheckForUpdates();
        }

        protected override void ApplyUpdate()
        {
            File.Copy(sourceFile, assembly, true);
        }

        protected override string GetAssemblyFile()
        {
            return assembly;
        }
    }
}
