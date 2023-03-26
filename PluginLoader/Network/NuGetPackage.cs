using ProtoBuf;
using System.Threading.Tasks;

namespace avaness.PluginLoader.Network
{
    [ProtoContract]
    public class NuGetPackage
    {

        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Version { get; set; }

        public NuGetPackage()
        {

        }

        public void Install()
        {
            Task.Run(InstallAsync).GetAwaiter().GetResult();
        }


        private async Task InstallAsync()
        {
            await Main.Instance.NuGet.InstallPackage(Name, Version);
        }
    }
}
