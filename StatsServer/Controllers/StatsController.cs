using avaness.StatsServer.Model;
using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace avaness.StatsServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly IStatsDatabase statsDatabase;

        public StatsController(IStatsDatabase statsDatabase)
        {
            this.statsDatabase = statsDatabase;
        }

        
        [HttpGet]
        public PluginStats Get(string playerHash)
        {
            return statsDatabase.GetStats(playerHash);
        }
    }
}