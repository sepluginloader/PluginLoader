using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
