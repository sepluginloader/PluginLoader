using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace avaness.StatsServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CanaryController : ControllerBase
    {
        private readonly IStatsDatabase statsDatabase;

        public CanaryController(IStatsDatabase statsDatabase)
        {
            this.statsDatabase = statsDatabase;
        }

        [HttpGet]
        public string Get()
        {
            // Make sure the server is not deadlocked
            statsDatabase.Canary();

            return "OK";
        }
    }
}