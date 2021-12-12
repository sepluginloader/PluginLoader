using System.Collections.Generic;
using avaness.PluginLoader.GUI;
using avaness.PluginLoader.Stats.Model;
using avaness.PluginLoader.Tools;

namespace avaness.PluginLoader.Stats
{
    public static class StatsClient
    {
        // API address
#if DEBUG
        private const string BaseUri = "http://localhost:5000";
#else
        private const string BaseUri = "https://pluginstats.ferenczi.eu";
#endif

        // API endpoints
        private static readonly string ConsentUri = $"{BaseUri}/Consent";
        private static readonly string StatsUri = $"{BaseUri}/Stats";
        private static readonly string TrackUri = $"{BaseUri}/Track";
        private static readonly string VoteUri = $"{BaseUri}/Vote";

        // Hashed Steam ID of the player
        private static string PlayerHash => playerHash ??= Tools.Tools.Sha1HexDigest($"{Tools.Tools.GetSteamId()}").Substring(0, 20);
        private static string playerHash;

        // Latest voting token received
        private static string votingToken;

        public static bool Consent(bool consent)
        {
            if (consent)
                LogFile.WriteLine($"Registering player consent on the statistics server");
            else
                LogFile.WriteLine($"Withdrawing player consent, removing user data from the statistics server");

            var consentRequest = new ConsentRequest()
            {
                PlayerHash = PlayerHash,
                Consent = consent
            };

            return SimpleHttpClient.Post(ConsentUri, consentRequest);
        }

        public static PluginStats DownloadStats()
        {

            if (!PlayerConsent.ConsentGiven)
            {
                LogFile.WriteLine($"Downloading plugin statistics anonymously (it does not allow for voting)");
                votingToken = null;
                return SimpleHttpClient.Get<PluginStats>(StatsUri);
            }

            LogFile.WriteLine($"Downloading plugin statistics, ratings and votes for " + PlayerHash);

            var parameters = new Dictionary<string, string> {["playerHash"] = PlayerHash};
            var pluginStats = SimpleHttpClient.Get<PluginStats>(StatsUri, parameters);

            votingToken = pluginStats?.VotingToken;

            return pluginStats;
        }

        public static bool Track(string[] pluginIds)
        {
            var trackRequest = new TrackRequest
            {
                PlayerHash = PlayerHash,
                EnabledPluginIds = pluginIds
            };

            return SimpleHttpClient.Post(TrackUri, trackRequest);
        }

        public static PluginStat Vote(string pluginId, int vote)
        {
            if (votingToken == null)
            {
                LogFile.WriteLine($"Voting token is not available, cannot vote");
                return null;
            }

            LogFile.WriteLine($"Voting {vote} on plugin {pluginId}");
            var voteRequest = new VoteRequest
            {
                PlayerHash = PlayerHash,
                PluginId = pluginId,
                VotingToken = votingToken,
                Vote = vote
            };

            var stat = SimpleHttpClient.Post<PluginStat, VoteRequest>(VoteUri, voteRequest);
            return stat;
        }
    }
}