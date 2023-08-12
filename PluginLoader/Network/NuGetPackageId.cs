using NuGet.Packaging.Core;
using NuGet.Versioning;
using ProtoBuf;
using System.Xml.Serialization;

namespace avaness.PluginLoader.Network
{
    [ProtoContract]
    public class NuGetPackageId
    {
        [ProtoMember(1)]
        [XmlElement]
        public string Name { get; set; }

        [ProtoIgnore]
        [XmlAttribute("Include")]
        public string NameAttribute
        {
            get => Name;
            set => Name = value;
        }

        [ProtoMember(2)]
        [XmlElement]
        public string Version { get; set; }

        [ProtoIgnore]
        [XmlAttribute("Version")]
        public string VersionAttribute
        {
            get => Version;
            set => Version = value;
        }

        public bool TryGetIdentity(out PackageIdentity id)
        {
            id = null;
            if(string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Version))
                return false;

            NuGetVersion version;
            if (!NuGetVersion.TryParse(Version, out version))
                return false;

            id = new PackageIdentity(Name, version);
            return true;
        }
    }
}
