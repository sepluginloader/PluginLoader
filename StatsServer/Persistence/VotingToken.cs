using System;

namespace avaness.StatsServer.Persistence;

public class VotingToken
{
    public readonly DateTime Created;
    public readonly Guid Guid;

    public VotingToken()
    {
        Created = DateTime.Now;
        Guid = Guid.NewGuid();
    }
}