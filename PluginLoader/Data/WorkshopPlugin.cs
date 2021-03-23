using System.IO;
using VRage;

namespace avaness.PluginLoader.Data
{
    public class WorkshopPlugin : SteamPlugin
    {
        public override string Source => MyTexts.GetString(MyCommonTexts.Workshop);
        protected override string HashFile => "hash.txt";

        private string assembly;

        protected WorkshopPlugin()
        {

        }

        public WorkshopPlugin(LogFile log, ulong id, string pluginFile) : base(log, id, pluginFile)
		{ }

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

        protected override string GetName()
        {
            string name = Path.GetFileNameWithoutExtension(sourceFile).Replace('_', ' ');
            if (string.IsNullOrWhiteSpace(name))
                return Id;
            else
                return name;
        }

        protected override void ApplyUpdate()
        {
            if (PluginList.Validate(WorkshopId, sourceFile, out string hash))
                File.Copy(sourceFile, assembly, true);
            else
                ErrorSecurity(hash);
        }

        protected override string GetAssemblyFile()
        {
            return assembly;
        }
    }
}
