using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using avaness.StatsServer.Model;

namespace avaness.StatsServer.Persistence
{
    public class PluginStatData
    {
        // Clean up usage entries after this many days of player inactivity
        private const int CleanupAfterDays = 60;

        // Plugin Id as in Plugin Loader
        public string Id { get; set; }

        // Date of last cleanup when
        public string LastCleanup { get; set; }

        // Players who had this plugin enabled anytime in the past CleanupAfterDays days
        // PlayerHash => LastDayUsed in ISO 8601 YYYY-MM-DD format
        public Dictionary<string, string> Players { get; set; } = new();

        // Set of players (PlayerHash) who upvoted the plugin
        public HashSet<string> Upvotes { get; set; } = new();

        // Set of players (PlayerHash) who downvoted the plugin
        public HashSet<string> Downvotes { get; set; } = new();

        // Name of the JSON file this object is serialized into when the database is saved to disk
        [JsonIgnore]
        public string FileName => fileNameCache ??= FormatFilename(Id);
        private string fileNameCache;
        private static string FormatFilename(string pluginId) => $"{Tools.Tools.SanitizeFileName(pluginId)}.json";

        // True if any of the usage or voting data has been modified since the object is initially
        // loaded or last saved, used only to eliminate unnecessary saving (optimization)
        private bool modified;

        public PluginStatData()
        {
        }

        public PluginStatData(string id)
        {
            Id = id;
        }

        private string JsonPath => FormatPath(FileName);

        public static string FormatPath(string fileName)
        {
            return Path.Combine(Config.PluginStatsDir, fileName);
        }

        public void MarkModified()
        {
            modified = true;
        }

        public void CleanupExpiredUses()
        {
            var today = DateTime.Today;
            var todayStr = Tools.Tools.FormatDateIso8601(today);
            if (LastCleanup == todayStr)
                return;

            LastCleanup = todayStr;

            var cutoffDateStr = Tools.Tools.FormatDateIso8601(today.AddDays(-CleanupAfterDays));
            var expiredUses = Players
                .Where(p => string.Compare(p.Value, cutoffDateStr, StringComparison.InvariantCulture) < 0)
                .Select(p => p.Key)
                .ToList();

            if (expiredUses.Count == 0)
                return;

            foreach (var playerHash in expiredUses)
                Players.Remove(playerHash);

            modified = true;
        }

        public void Save()
        {
            CleanupExpiredUses();

            if (File.Exists(JsonPath) && !modified)
                return;

            modified = false;

            var json = JsonSerializer.Serialize(this, Tools.Tools.JsonOptions);

            var newPath = JsonPath + ".new";
            File.WriteAllText(newPath, json);
            File.Move(newPath, JsonPath, true);
        }

        public static PluginStatData Load(string pluginId)
        {
            var fileName = FormatFilename(pluginId);
            var path = FormatPath(fileName);

            if (!File.Exists(path))
                return new PluginStatData(pluginId);

            var json = File.ReadAllText(path);
            var pluginStatData = JsonSerializer.Deserialize<PluginStatData>(json);
            Debug.Assert(pluginStatData != null);

            pluginStatData.CleanupExpiredUses();

            return pluginStatData;
        }

        public void ReportUse(string playerHash, string day)
        {
            Players[playerHash] = day;
            MarkModified();
        }

        public int GetVote(string playerHash) => Upvotes.Contains(playerHash) ? 1 : Downvotes.Contains(playerHash) ? -1 : 0;

        public PluginStat SetVote(string playerHash, int vote)
        {
            if (vote > 0)
                Upvotes.Add(playerHash);
            else
                Upvotes.Remove(playerHash);

            if (vote < 0)
                Downvotes.Add(playerHash);
            else
                Downvotes.Remove(playerHash);

            MarkModified();

            var tried = Players.ContainsKey(playerHash);
            return GetStat(tried, vote);
        }

        private PluginStat GetStat(bool tried, int vote)
        {
            return new PluginStat(
                Players.Count,
                Upvotes.Count,
                Downvotes.Count,
                tried,
                vote
            );
        }

        public PluginStat GetStat(string playerHash)
        {
            var tried = playerHash != null && Players.ContainsKey(playerHash);
            var vote = string.IsNullOrEmpty(playerHash) ? 0 : GetVote(playerHash);
            return GetStat(tried, vote);
        }

        public void ForgetPlayer(string playerHash)
        {
            var removed = Players.Remove(playerHash);
            removed = Downvotes.Remove(playerHash) || removed;
            removed = Upvotes.Remove(playerHash) || removed;

            if (removed)
                MarkModified();
        }
    }
}