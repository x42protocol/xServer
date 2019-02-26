using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using x42.Feature.API.Requirements;
using X42.Server;

namespace X42.Controllers
{
    /// <summary>
    ///     Controller providing HTML Dashboard
    /// </summary>
    [Route("")]
    [Route("[controller]")]
    [Authorize(Policy = Policy.PrivateAccess)]
    public class DashboardController : Controller
    {
        private readonly IX42Server x42Server;

        public DashboardController(IX42Server x42Server)
        {
            this.x42Server = x42Server;
        }

        /// <summary>
        ///     Returns a web page to act as a dashboard
        /// </summary>
        /// <returns>text/html content</returns>
        [HttpGet]
        [Route("")] // the endpoint name
        [Route("Stats")]
        public IActionResult Stats()
        {
            string content = new X42Server().LastLogOutput;
            return Content(content);
        }
    }
}