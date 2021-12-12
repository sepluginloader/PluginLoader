using avaness.StatsServer.Model;
using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace avaness.StatsServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TrackController : ControllerBase
    {
        [HttpPost]
        public void Post(TrackRequest request)
        {
            StatsDatabase.Instance?.Track(request);
        }
    }
}