using avaness.StatsServer.Model;
using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace avaness.StatsServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConsentController : ControllerBase
    {
        private readonly IStatsDatabase statsDatabase;

        public ConsentController(IStatsDatabase statsDatabase)
        {
            this.statsDatabase = statsDatabase;
        }
        
        [HttpPost]
        public void Post(ConsentRequest request)
        {
            statsDatabase.Consent(request);
        }
    }
}