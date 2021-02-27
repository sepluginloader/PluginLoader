using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace avaness.PluginLoader.Data
{
    public abstract class SteamPlugin : PluginData
    {
        public ulong WorkshopId { get; }

        protected SteamPlugin()
        {
        }

        public SteamPlugin(ulong id) : base(id.ToString())
        {

        }
    }
}
