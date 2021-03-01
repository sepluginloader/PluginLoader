using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using VRage.Plugins;

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
                    default:
                        return "";
                }
            }
        }

        public virtual string Id { get; set; }
        public bool Enabled { get; set; }

        protected PluginData()
        {

        }

        public PluginData(string id)
        {
            Id = id;
        }

        public abstract string GetDllFile();

        public bool TryLoadAssembly(LogFile log, out Assembly a)
        {
            a = null;
            string dll = GetDllFile();
            if (dll == null)
            {
                log.WriteLine("Failed to load " + ToString());
                return false;
            }

            try
            {
                a = Assembly.LoadFile(dll);
                return true;
            }
            catch (Exception e) 
            {
                log.WriteLine($"Failed to load {ToString()} because of an error: " + e);
            }
            return false;
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
    }
}