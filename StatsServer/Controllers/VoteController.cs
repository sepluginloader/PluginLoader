using avaness.StatsServer.Model;
using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace avaness.StatsServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VoteController : ControllerBase
    {
        private readonly IStatsDatabase statsDatabase;

        public VoteController(IStatsDatabase statsDatabase)
        {
            this.statsDatabase = statsDatabase;
        }
        
        [HttpPost]
        public PluginStat Post(VoteRequest request)
        {
            return statsDatabase.Vote(request);
        }
    }
}