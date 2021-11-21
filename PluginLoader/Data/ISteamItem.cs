using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace avaness.PluginLoader.Data
{
    public interface ISteamItem
    {
        string Id { get; }
        ulong WorkshopId { get; }
    }
}
