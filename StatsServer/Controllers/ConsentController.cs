using avaness.StatsServer.Model;
using avaness.StatsServer.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace avaness.StatsServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConsentController : ControllerBase
    {
        [HttpPost]
        public void Post(ConsentRequest request)
        {
            StatsDatabase.Instance?.Consent(request);
        }
    }
}