using System;
using avaness.StatsServer.Model;

namespace avaness.StatsServer.Persistence;

public interface IStatsDatabase : IDisposable
{
    void Save();
    void Canary();
    void Consent(ConsentRequest request);
    PluginStats GetStats(string playerHash);
    void Track(TrackRequest request);
    PluginStat Vote(VoteRequest request);
}