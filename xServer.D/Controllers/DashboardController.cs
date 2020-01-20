using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X42.Feature.API.Requirements;
using X42.Server;

namespace X42.Controllers
{
    /// <inheritdoc />
    /// <summary>
    ///     Controller providing HTML Dashboard
    /// </summary>
    [ApiController]
    [Route("")]
    [Authorize(Policy = Policy.PrivateAccess)]
    public class DashboardController : Controller
    {
        private readonly IxServer x42Server;

        public DashboardController(IxServer x42Server)
        {
            this.x42Server = x42Server;
        }

        /// <summary>
        ///     Returns the last iteration of the log output
        /// </summary>
        /// <returns>text/latest logs</returns>
        [HttpGet]
        [Route("/")]
        public IActionResult Log()
        {
            string content = x42Server.LastLogOutput;
            return Content(content);
        }
    }
}