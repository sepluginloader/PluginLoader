using Steamworks;
using System.Collections.Generic;

namespace avaness.PluginLoader
{
    public static class SteamAPI
    {
        public static bool IsSubscribed(ulong id)
        {
            EItemState state = (EItemState)SteamUGC.GetItemState(new PublishedFileId_t(id));
            return (state & EItemState.k_EItemStateSubscribed) == EItemState.k_EItemStateSubscribed;
        }
    }
}
