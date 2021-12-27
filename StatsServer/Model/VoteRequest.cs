// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace avaness.StatsServer.Model
{
    // Request data received from the Plugin Loader each time a user changes his/her vote on a plugin
    public class VoteRequest
    {
        // Id of the plugin
        public string PluginId { get; set; }

        // Obfuscated player identifier, see Track.PlayerHash
        public string PlayerHash { get; set; }

        // Voting token returned with the plugin stats
        public string VotingToken { get; set; }

        // Vote to store
        // +1: Upvote
        //  0: Clear vote
        // -1: Downvote
        public int Vote { get; set; }
    }
}