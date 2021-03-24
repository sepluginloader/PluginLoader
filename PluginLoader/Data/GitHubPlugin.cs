using ProtoBuf;
using System;

namespace avaness.PluginLoader.Data
{
    [ProtoContract]
    public class GitHubPlugin : PluginData
    {
        public override string Source => "GitHub";

        public override string FriendlyName => Name;

        [ProtoMember(1)]
        public string Name { get; set; } = "";

        public override string GetDllFile()
        {
            throw new NotImplementedException();
        }

        public override void Show()
        {
            throw new NotImplementedException();
        }
    }
}
