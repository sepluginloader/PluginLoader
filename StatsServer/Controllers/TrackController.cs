using avaness.StatsServer.Model;
using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace avaness.StatsServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrackController : ControllerBase
    {
        private readonly IStatsDatabase statsDatabase;

        public TrackController(IStatsDatabase statsDatabase)
        {
            this.statsDatabase = statsDatabase;
        }

        
        [HttpPost]
        public void Post(TrackRequest request)
        {
            statsDatabase.Track(request);
        }
    }
}