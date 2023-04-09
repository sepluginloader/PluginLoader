using Microsoft.CodeAnalysis;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace avaness.PluginLoader.Network
{
    [ProtoContract]
    public class NuGetPackage
    {

        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Version { get; set; }

        public List<MetadataReference> RoslynReferences { get; } = new List<MetadataReference>();

        public NuGetPackage()
        {

        }

        public void Install()
        {
            Task.Run(InstallAsync).GetAwaiter().GetResult();
        }


        private async Task InstallAsync()
        {
            string[] assemblies = await Main.Instance.NuGet.InstallPackage(Name, Version);
            RoslynReferences.Clear();
            foreach (string dll in assemblies)
            {
                if (IsValidDllReference(dll) && TryLoadDllReference(dll, out MetadataReference reference))
                    RoslynReferences.Add(reference);
            }
        }

        private bool TryLoadDllReference(string dll, out MetadataReference reference)
        {
            try
            {
                reference = MetadataReference.CreateFromFile(dll);
                return true;
            }
            catch
            {
                reference = null;
                return false;
            }
        }

        private bool IsValidDllReference(string dll)
        {
            return Path.HasExtension(dll) 
                && Path.GetExtension(dll).Equals(".dll", StringComparison.OrdinalIgnoreCase)
                && File.Exists(dll);
        }
    }
}
