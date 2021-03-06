using Sandbox.Graphics.GUI;
using System.IO;

namespace avaness.PluginLoader.Data
{
    public abstract class SteamPlugin : PluginData
    {
        public override string FriendlyName { get; }

        public ulong WorkshopId { get; }
        protected abstract string HashFile { get; }
        protected string root, sourceFile, hashFile;

        protected SteamPlugin()
        {
        }

        public SteamPlugin(LogFile log, ulong id, string sourceFile) : base(log, id.ToString())
        {
            WorkshopId = id;
            this.sourceFile = sourceFile;
            root = Path.GetDirectoryName(sourceFile);
            hashFile = Path.Combine(root, HashFile);

            // See https://www.jetbrains.com/help/rider/VirtualMemberCallInConstructor.html
            CheckForUpdates();

            // See https://www.jetbrains.com/help/rider/VirtualMemberCallInConstructor.html
            FriendlyName = GetName();
        }

        protected virtual void CheckForUpdates()
        {
            if (File.Exists(hashFile))
            {
                string oldHash = File.ReadAllText(hashFile);
                string newHash = LoaderTools.GetHash1(sourceFile);
                if (oldHash != newHash)
                    Status = PluginStatus.PendingUpdate;
            }
            else
            {
                Status = PluginStatus.PendingUpdate;
            }
        }

        protected abstract string GetName();

        public override string GetDllFile()
        {
            if (Status == PluginStatus.PendingUpdate)
            {
                log.WriteLine("Updating " + this);
                File.WriteAllText(hashFile, LoaderTools.GetHash1(sourceFile));
                ApplyUpdate();
                if (Status == PluginStatus.PendingUpdate)
                    Status = PluginStatus.Updated;
                else
                    return null;

            }
            string dll = GetAssemblyFile();
            if (dll == null || !File.Exists(dll))
                return null;
            return dll;
        }

        protected abstract void ApplyUpdate();
        protected abstract string GetAssemblyFile();

        public override void Show()
        {
            MyGuiSandbox.OpenUrl("https://steamcommunity.com/workshop/filedetails/?id=" + WorkshopId, UrlOpenMode.SteamOrExternalWithConfirm);
        }
    }
}
