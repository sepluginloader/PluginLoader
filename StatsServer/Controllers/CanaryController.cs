using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace avaness.StatsServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CanaryController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            // Make sure the server is not deadlocked
            StatsDatabase.Instance.Canary();

            return "OK";
        }
    }
}