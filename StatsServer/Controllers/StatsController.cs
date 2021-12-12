using avaness.StatsServer.Model;
using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace avaness.StatsServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatsController : ControllerBase
    {
        [HttpGet]
        public PluginStats Get(string playerHash)
        {
            return StatsDatabase.Instance?.GetStats(playerHash);
        }
    }
}