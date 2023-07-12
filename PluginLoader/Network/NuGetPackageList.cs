using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace avaness.PluginLoader.Network
{
    [ProtoContract]
    public class NuGetPackageList
    {
        [ProtoMember(1)]
        public string PackagesConfig { get; set; }

        [ProtoMember(2)]
        [XmlArray("Packages")]
        [XmlArrayItem("Package")]
        public NuGetPackageId[] PackageIds { get; set; }

        public string PackagesConfigNormalized => PackagesConfig?.Replace('\\', '/').TrimStart('/');

        public bool HasPackages => !string.IsNullOrWhiteSpace(PackagesConfig) || (PackageIds != null && PackageIds.Length > 0);
    }
}
