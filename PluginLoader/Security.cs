using System.Collections.Generic;

namespace avaness.PluginLoader
{
    internal static class Security
    {
        private readonly static HashSet<ulong> whitelistItemIds = new HashSet<ulong>
        {
            // Workshop
            2292390607, // Tool Switcher
            2413859055, // SteamWorkshopFix
            2413918072, // SEWorldGenPlugin v2
            2414532651, // DecalFixPlugin
            2415983416, // Multigrid Projector
            2425805190, // MorePPSettings
            // SEPM - Most of these are old or broken
            2004495632, // BlockPicker
            1937528740, // GridFilter
            2029854486, // RemovePlanetSizeLimits
            2171994463, // ClientFixes
            2156683844, // SEWorldGenPlugin
            1937530079, // Mass Rename
            2037606896, // CameraLCD
        };

        private readonly static HashSet<string> whitelistItemSha = new HashSet<string>()
        {
            "fa6d204bcb706bb5ba841e06e19b2793f324d591093e67ca0752d681fb5e6352", // Jump Selector Plugin
            "275afaead0e5a7d83b0c5be5f13fe67b8b96e375dba4fd5cf4976f6dbce58e81"
        };

        public static bool Validate(ulong steamId, string file, out string sha256)
        {
            sha256 = null;
            if (whitelistItemIds.Contains(steamId))
                return true;
            sha256 = LoaderTools.GetHash256(file);
            return whitelistItemSha.Contains(sha256);
        }
    }
}
