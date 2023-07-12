using NuGet.Packaging.Core;
using NuGet.Versioning;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace avaness.PluginLoader.Network
{
    [ProtoContract]
    public class NuGetPackageId
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Version { get; set; }

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
