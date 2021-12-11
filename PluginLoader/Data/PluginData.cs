using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace avaness.PluginLoader.Data
{
    [XmlInclude(typeof(WorkshopPlugin))]
    [XmlInclude(typeof(SEPMPlugin))]
    [XmlInclude(typeof(GitHubPlugin))]
    [XmlInclude(typeof(ModPlugin))]
    [ProtoContract]
    [ProtoInclude(100, typeof(SteamPlugin))]
    [ProtoInclude(103, typeof(GitHubPlugin))]
    [ProtoInclude(104, typeof(ModPlugin))]
    public abstract class PluginData : IEquatable<PluginData>
    {
        public abstract string Source { get; }

        [XmlIgnore]
        public Version Version { get; protected set; }

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

        [ProtoMember(1)]
        public virtual string Id { get; set; }

        [ProtoMember(2)]
        public string FriendlyName { get; set; } = "Unknown";

        [ProtoMember(3)]
        public bool Hidden { get; set; } = false;

        [ProtoMember(4)]
        public string GroupId { get; set; }

        [ProtoMember(5)]
        public string Tooltip { get; set; }

        [ProtoMember(6)]
        public string Author { get; set; }

        [XmlIgnore]
        public List<PluginData> Group { get; } = new List<PluginData>();

        [XmlIgnore]
        //Max rating is 10 half stars. Starts at 0.
        public int Rating { get; set; }

        [XmlIgnore]
        public bool Enabled => Main.Instance.Config.IsEnabled(Id);

        [XmlIgnore]
        public bool EnableAfterRestart;

        [XmlIgnore]
        public bool Modified => Enabled != EnableAfterRestart;

        [XmlIgnore]
        public string Key => $"{Source}|{Id}";

        protected PluginData()
        {
        }

        #region IMPORTANT
        //Code setup for code integration with stats system.
        //It is highly recommended to check the rating status locally as GetRateStatus would not sync instantly with the server.

        //Please set up a way to check with the server if the user rated.
        public virtual RateStatus GetRateStatus()
        {
            return RateStatus.None;
        }

        public enum RateStatus
        {
            None = 0,
            RatedUp = 1,
            RatedDown = -1
        }

        //Please setup how you want this to work
        //Event that occurs when user rates plugin. 
        public virtual void Rate(RateStatus ratetype)
        {
            //If the user rated up.
            if (ratetype == RateStatus.RatedUp)
            {

            }
            //If the user rated down.
            else if (ratetype == RateStatus.RatedDown)
            {

            }
        }
        #endregion

        public abstract Assembly GetAssembly();

        public virtual bool TryLoadAssembly(out Assembly a)
        {
            if (Status == PluginStatus.Error)
            {
                a = null;
                return false;
            }

            try
            {
                // Get the file path
                a = GetAssembly();
                if (Status == PluginStatus.Blocked)
                {
                    return false;
                }

                if (a == null)
                {
                    LogFile.WriteLine("Failed to load " + ToString());
                    Error();
                    return false;
                }

                // Precompile the entire assembly in order to force any missing method exceptions
                LogFile.WriteLine("Precompiling " + a);
                LoaderTools.Precompile(a);
                return true;
            }
            catch (Exception e)
            {
                string name = ToString();
                LogFile.WriteLine($"Failed to load {name} because of an error: " + e);
                if (e is MissingMemberException)
                {
                    LogFile.WriteLine($"Is {name} up to date?");
                }

                LogFile.Flush();
                Error();
                a = null;
                return false;
            }
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
            MessageBox.Show(LoaderTools.GetMainForm(), $"Unable to load the plugin {this} because it is not whitelisted!", "Plugin Loader", MessageBoxButtons.OK, MessageBoxIcon.Error);
            LogFile.WriteLine("Error: " + this + " with an sha256 of " + hash + " is not on the whitelist!");
        }

        public abstract void Show();
    }
}