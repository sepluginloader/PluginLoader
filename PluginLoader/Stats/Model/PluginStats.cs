using System.Collections.Generic;

namespace avaness.PluginLoader.Stats.Model
{
    // Statistics for all plugins
    public class PluginStats
    {
        // Key: pluginId
        public Dictionary<string, PluginStat> Stats { get; set; } = new Dictionary<string, PluginStat>();

        // Token the player is required to present for voting (making it harder to spoof votes)
        public string VotingToken { get; set; }
    }
}