// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace avaness.StatsServer.Model
{
    // Statistics for a single plugin
    public class PluginStat
    {
        // Give stars only if the total number of upvotes and downvotes reaches this minimum
        private const int MinVotesForStars = 50;

        // Number of players who successfully started SE with this plugin enabled anytime during the past CleanupAfterDays days
        public int Players { get; set; }

        // Total number of upvotes and downvotes since the beginning (votes do not expire)
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }

        // Whether the requesting player tried the plugin
        public bool Tried { get; set; }

        // Current vote of the requesting player
        // +1: Upvoted
        //  0: No vote (or cleared it)
        // -1: Downvoted
        public int Vote { get; set; }

        // Number of half stars [1-10], zero if there are not enough votes on the plugin yet
        public int Rating
        {
            get
            {
                if (Upvotes + Downvotes < MinVotesForStars)
                    return 0;

                var upvoted = Upvotes / (float)(Upvotes + Downvotes);

                var rating = (int)((upvoted - 0.7 + 0.03) / 0.03);

                return rating switch
                {
                    < 1 => 1,
                    > 10 => 10,
                    _ => rating
                };
            }
        }

        public PluginStat(int players, int upvotes, int downvotes, bool tried, int vote)
        {
            Players = players;
            Upvotes = upvotes;
            Downvotes = downvotes;
            Tried = tried;
            Vote = vote;
        }
    }
}