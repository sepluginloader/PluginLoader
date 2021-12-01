using System.Collections.Generic;

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace avaness.StatsServer.Model
{
    // Statistics for all plugins
    public class PluginStats
    {
        // Key: pluginId
        public Dictionary<string, PluginStat> Stats { get; } = new();

        // Token the player is required to present for voting (making it harder to spoof votes)
        public string VotingToken { get; set; }
    }
}