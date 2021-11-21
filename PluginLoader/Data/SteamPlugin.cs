using ProtoBuf;
using Sandbox.Graphics.GUI;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace avaness.PluginLoader.Data
{
    [ProtoContract]
    [ProtoInclude(101, typeof(SEPMPlugin))]
    [ProtoInclude(102, typeof(WorkshopPlugin))]
    public abstract class SteamPlugin : PluginData, ISteamItem
    {
        [XmlIgnore]
        public ulong WorkshopId { get; private set; }

        [XmlArray]
        [ProtoMember(1)]
        public string[] AllowedHashes { get; set; }

        public override string Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
                WorkshopId = ulong.Parse(Id);
            }
        }

        protected abstract string HashFile { get; }
        protected string root, sourceFile, hashFile;

        protected SteamPlugin()
        {
        }

        public void Init(string sourceFile)
        {
            Status = PluginStatus.None;
            this.sourceFile = sourceFile;
            root = Path.GetDirectoryName(sourceFile);
            hashFile = Path.Combine(root, HashFile);

            CheckForUpdates();
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

        public override Assembly GetAssembly()
        {
            if (Status == PluginStatus.PendingUpdate)
            {
                LogFile.WriteLine("Updating " + this);
                ApplyUpdate();
                if (Status == PluginStatus.PendingUpdate)
                {
                    File.WriteAllText(hashFile, LoaderTools.GetHash1(sourceFile));
                    Status = PluginStatus.Updated;
                }
                else
                {
                    return null;
                }

            }
            string dll = GetAssemblyFile();
            if (dll == null || !File.Exists(dll))
                return null;
            if (!VerifyAllowed(dll))
                return null;
            Assembly a = Assembly.LoadFile(dll);
            Version = a.GetName().Version;
            return a;
        }

        protected abstract void ApplyUpdate();
        protected abstract string GetAssemblyFile();

        public override void Show()
        {
            MyGuiSandbox.OpenUrl("https://steamcommunity.com/workshop/filedetails/?id=" + Id, UrlOpenMode.SteamOrExternalWithConfirm);
        }

        private bool VerifyAllowed(string dll)
        {
            if (AllowedHashes == null || AllowedHashes.Length == 0)
                return true;

            string hash = LoaderTools.GetHash256(dll);
            foreach(string s in AllowedHashes)
            {
                if (s == hash)
                    return true;
            }

            ErrorSecurity(hash);
            return false;
        }
    }
}
