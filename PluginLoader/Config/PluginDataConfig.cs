using System.Xml.Serialization;

namespace avaness.PluginLoader.Config
{
    [XmlInclude(typeof(LocalFolderConfig))]
    [XmlInclude(typeof(GitHubPluginConfig))]
    public abstract class PluginDataConfig
    {
        public string Id { get; set; }
    }
}
