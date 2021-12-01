using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using avaness.StatsServer.Model;
using Microsoft.AspNetCore.Builder;

namespace avaness.StatsServer.Persistence
{
    public class StatsDatabase : IDisposable
    {
        private const int VotingTokenExpiration = 1; // Hours
        private const int VotingTokenCleanupPeriod = 600; // Seconds

#pragma warning disable CA2211
        public static StatsDatabase Instance;
#pragma warning restore CA2211

        public static string PluginStatsDir => Path.Combine(Config.DataDir, "PluginStats");
        private static string PluginMapPath => Path.Combine(Config.DataDir, "Plugins.json");
        private static string CanaryPath => Path.Combine(Config.DataDir, "Canary.txt");

        private readonly PlayerConsents playerConsents = new();

        private readonly Dictionary<string, PluginStatData> pluginStatsData = new();

        private class VotingToken
        {
            public readonly DateTime Created;
            public readonly Guid Guid;

            public VotingToken()
            {
                Created = DateTime.Now;
                Guid = Guid.NewGuid();
            }
        }

        private readonly Dictionary<string, VotingToken> votingTokens = new();

        private DateTime nextVotingTokenCleanup = DateTime.Now;

        public StatsDatabase()
        {
            Load();
            Instance = this;
        }

#pragma warning disable CA1816
        public void Dispose()
        {
            Instance = null;
            Save();
        }
#pragma warning restore CA1816

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public void Canary()
        {
            using var log = new StreamWriter(CanaryPath);

            log.Write($"{DateTime.Now:O}: Canary request received{Environment.NewLine}");
            log.Flush();

            TimeAcquiringLock(log, pluginStatsData, nameof(pluginStatsData));
            TimeAcquiringLock(log, votingTokens, nameof(votingTokens));
        }

        private static void TimeAcquiringLock(StreamWriter log, object obj, string name)
        {
            var duration = 0.0;

            log.Write($"{DateTime.Now:O}: lock ({name}) acquiring...{Environment.NewLine}");

            var started = DateTime.Now;
            lock (obj)
            {
                duration = (DateTime.Now - started).TotalMilliseconds;
            }

            log.Write($"{DateTime.Now:O}: lock ({name}) acquired in {duration:0.000}ms{Environment.NewLine}");
        }

        public void Save()
        {
            Directory.CreateDirectory(PluginStatsDir);

            lock (pluginStatsData)
            {
                playerConsents.Save();

                SavePluginMap();
                SavePluginStatsData();
            }

            Canary();
        }

        private void SavePluginMap()
        {
            var pluginMap = pluginStatsData.ToDictionary(p => p.Value.FileName, p => p.Value.Id);
            if (pluginMap.Count != pluginStatsData.Count)
                throw new InvalidDataException("Database file name collision (this can only happen with near zero probability)");

            var json = JsonSerializer.Serialize(pluginMap, Tools.Tools.JsonOptions);

            var newPath = PluginMapPath + ".new";
            File.WriteAllText(newPath, json);
            File.Move(newPath, PluginMapPath, true);
        }

        private void CleanupExpiredVotingTokens()
        {
            var now = DateTime.Now;
            if (now < nextVotingTokenCleanup)
                return;

            nextVotingTokenCleanup = now.AddSeconds(VotingTokenCleanupPeriod);

            var cutoff = now.AddHours(-VotingTokenExpiration);

            var expiredVotingTokens = votingTokens
                .Where(p => p.Value.Created < cutoff)
                .Select(p => p.Key)
                .ToList();

            if (expiredVotingTokens.Count == 0)
                return;

            foreach (var playerHash in expiredVotingTokens)
                votingTokens.Remove(playerHash);
        }

        private void SavePluginStatsData()
        {
            CleanupExpiredVotingTokens();

            foreach (var pluginStat in pluginStatsData.Values)
                pluginStat.Save();
        }

        private void Load()
        {
            lock (pluginStatsData)
            {
                playerConsents.Load();

                var pluginMap = LoadPluginMap();
                LoadPluginStatsData(pluginMap);
            }

            Canary();
        }

        private static Dictionary<string, string> LoadPluginMap()
        {
            var json = File.Exists(PluginMapPath) ? File.ReadAllText(PluginMapPath) : "{}";
            var pluginMap = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (pluginMap == null)
                throw new InvalidDataException($"Could not deserialize plugin map JSON loaded from: {PluginMapPath}");

            return pluginMap;
        }

        private void LoadPluginStatsData(Dictionary<string, string> pluginMap)
        {
            pluginStatsData.Clear();
            foreach (var pluginId in pluginMap.Values)
            {
                var pluginStatData = PluginStatData.Load(pluginId);
                Debug.Assert(pluginStatData.Id == pluginId);
                pluginStatsData[pluginId] = pluginStatData;
            }
        }

        public void Consent(ConsentRequest request)
        {
            if (!Tools.Tools.ValidatePlayerHash(request.PlayerHash))
                return;

            playerConsents.Register(request.PlayerHash, request.Consent);

            if (!playerConsents.Verify(request.PlayerHash))
                ForgetPlayer(request.PlayerHash);
        }

        private void ForgetPlayer(string playerHash)
        {
            lock (pluginStatsData)
            {
                votingTokens.Remove(playerHash);

                foreach (var pluginStatData in pluginStatsData.Values)
                    pluginStatData.ForgetPlayer(playerHash);
            }
        }

        public PluginStats GetStats(string playerHash)
        {
            var pluginStats = new PluginStats();

            if (!string.IsNullOrEmpty(playerHash))
            {
                if (!Tools.Tools.ValidatePlayerHash(playerHash))
                    return null;

                if (playerConsents.Verify(playerHash))
                {
                    var votingToken = new VotingToken();

                    lock (votingTokens)
                        votingTokens[playerHash] = votingToken;

                    pluginStats.VotingToken = votingToken.Guid.ToString();
                }
            }

            lock (pluginStatsData)
            {
                foreach (var pluginStatData in pluginStatsData.Values)
                    pluginStats.Stats[pluginStatData.Id] = pluginStatData.GetStat(playerHash);
            }

            return pluginStats;
        }

        public void Track(TrackRequest request)
        {
            if (request == null)
                return;

            if (!playerConsents.Verify(request.PlayerHash))
                return;

            var today = Tools.Tools.FormatDateIso8601(DateTime.Today);

            lock (pluginStatsData)
            {
                foreach (var pluginId in request.EnabledPluginIds)
                {
                    if (!pluginStatsData.TryGetValue(pluginId, out var pluginStat))
                        pluginStat = pluginStatsData[pluginId] = new PluginStatData(pluginId);

                    pluginStat.ReportUse(request.PlayerHash, today);
                }
            }
        }

        public PluginStat Vote(VoteRequest request)
        {
            if (request == null)
                return null;

            if (!playerConsents.Verify(request.PlayerHash))
                return null;

            // FIXME: Consider logging the failure cases when null is returned below (they may be a precursor to a voting attack)

            // Allow voting only if the player can present a valid token (makes it harder to spoof votes)
            lock (votingTokens)
            {
                if (!votingTokens.TryGetValue(request.PlayerHash, out var votingToken))
                    return null;

                if (request.VotingToken != votingToken.Guid.ToString())
                    return null;
            }

            lock (pluginStatsData)
            {
                if (!pluginStatsData.TryGetValue(request.PluginId, out var pluginStat))
                    pluginStat = pluginStatsData[request.PluginId] = new PluginStatData(request.PluginId);

                // Allow voting only if the player has used the plugin recently (sanity and fraud check)
                if (!pluginStat.Players.ContainsKey(request.PlayerHash))
                    return null;

                var stat = pluginStat.SetVote(request.PlayerHash, request.Vote);
                return stat;
            }
        }
    }
}