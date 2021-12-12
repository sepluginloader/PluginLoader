// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace avaness.StatsServer.Model
{
    // Request data received from the Plugin Loader each time the game is started
    public class TrackRequest
    {
        // Hash of the player's Steam ID
        public string PlayerHash { get; set; }

        // Ids of enabled plugins when the game started
        public string[] EnabledPluginIds { get; set; }
    }
}