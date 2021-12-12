using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace avaness.StatsServer.Persistence
{
    public class PlayerConsents
    {
        private static string PlayerConsentsPath => Path.Combine(Config.DataDir, "PlayerConsents.json");

        // Player consent dates
        private Dictionary<string, string> playerConsents;

        // True if a consent has been recorded or revoked since the dictionary was loaded or last saved
        private bool modified;

        public void Save()
        {
            if (!modified)
                return;

            modified = false;

            var json = JsonSerializer.Serialize(playerConsents, Tools.Tools.JsonOptions);

            var newPath = PlayerConsentsPath + ".new";
            File.WriteAllText(newPath, json);
            File.Move(newPath, PlayerConsentsPath, true);
        }

        public void Load()
        {
            var json = File.Exists(PlayerConsentsPath) ? File.ReadAllText(PlayerConsentsPath) : "{}";
            playerConsents = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }

        public void Register(string playerHash, bool consent)
        {
            if (consent == playerConsents.ContainsKey(playerHash))
                return;

            if (consent)
            {
                playerConsents[playerHash] = Tools.Tools.FormatDateIso8601(DateTime.Today);
                modified = true;
                return;
            }

            if (playerConsents.Remove(playerHash))
                modified = true;
        }

        public bool Verify(string playerHash) => playerConsents.ContainsKey(playerHash);
    }
}