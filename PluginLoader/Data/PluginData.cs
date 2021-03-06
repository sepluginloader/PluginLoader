using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace avaness.PluginLoader.Data
{
    public abstract class PluginData : IEquatable<PluginData>
    {
        public abstract string Source { get; }
        public abstract string FriendlyName { get; }

        [XmlIgnore]
        public virtual PluginStatus Status { get; set; } = PluginStatus.None;
        public virtual string StatusString
        {
            get
            {
                switch (Status)
                {
                    case PluginStatus.PendingUpdate:
                        return "Pending Update";
                    case PluginStatus.Updated:
                        return "Updated";
                    case PluginStatus.Error:
                        return "Error!";
                    case PluginStatus.Blocked:
                        return "Not whitelisted!";
                    default:
                        return "";
                }
            }
        }

        public virtual string Id { get; set; }
        public bool Enabled { get; set; }

        protected LogFile log;

        protected PluginData()
        {

        }

        public PluginData(LogFile log, string id)
        {
            this.log = log;
            Id = id;
        }

        public abstract string GetDllFile();

        public bool TryLoadAssembly(out Assembly a)
        {
            a = null;

            try
            {
                // Get the file path
                string dll = GetDllFile();
                if (Status == PluginStatus.Blocked)
                    return false;

                if (dll == null)
                {
                    log.WriteLine("Failed to load " + ToString());
                    Error();
                    return false;
                }

                // Verify the file
                if (this is SteamPlugin steam && !Security.Validate(steam.WorkshopId, dll, out string hash))
                {
                    ErrorSecurity(hash);
                    return false;
                }

                // Load the assembly
                a = Assembly.LoadFile(dll);

                // Precompile the entire assembly in order to force any missing method exceptions
                log.WriteLine("Precompiling " + a);
                LoaderTools.Precompile(a);
                return true;
            }
            catch (Exception e)
            {
                string name = ToString();
                log.WriteLine($"Failed to load {name} because of an error: " + e);
                if (e is MissingMemberException)
                    log.WriteLine($"Is {name} up to date?");
                log.Flush();
                Error();
                return false;
            }
        }


        public virtual void CopyFrom(PluginData other)
        {
            Enabled = other.Enabled;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PluginData);
        }

        public bool Equals(PluginData other)
        {
            return other != null &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + EqualityComparer<string>.Default.GetHashCode(Id);
        }

        public static bool operator ==(PluginData left, PluginData right)
        {
            return EqualityComparer<PluginData>.Default.Equals(left, right);
        }

        public static bool operator !=(PluginData left, PluginData right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return Id + '|' + FriendlyName;
        }

        public void Error()
        {
            Status = PluginStatus.Error;
            MessageBox.Show(LoaderTools.GetMainForm(), $"The plugin '{this}' caused an error. It is recommended that you disable this plugin and restart. The game may be unstable beyond this point. See loader.log or the game log for details.", "Plugin Loader", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected void ErrorSecurity(string hash)
        {
            Status = PluginStatus.Blocked;
            MessageBox.Show(LoaderTools.GetMainForm(), $"Unable to load or update the plugin {this} because it is not whitelisted!", "Plugin Loader", MessageBoxButtons.OK, MessageBoxIcon.Error);
            log.WriteLine("Error: " + this + " with an sha256 of " + hash + " is not on the whitelist!");
        }

        public abstract void Show();
    }
}