using avaness.StatsServer.Model;
using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace avaness.StatsServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VoteController : ControllerBase
    {
        [HttpPost]
        public PluginStat Post(VoteRequest request)
        {
            return StatsDatabase.Instance?.Vote(request);
        }
    }
}