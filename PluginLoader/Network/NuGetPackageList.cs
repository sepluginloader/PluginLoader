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
        public string Config { get; set; }

        [ProtoMember(2)]
        [XmlElement("PackageReference", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public NuGetPackageId[] PackageIds { get; set; }

        public string PackagesConfigNormalized => Config?.Replace('\\', '/').TrimStart('/');

        public bool HasPackages => !string.IsNullOrWhiteSpace(Config) || (PackageIds != null && PackageIds.Length > 0);
    }
}
