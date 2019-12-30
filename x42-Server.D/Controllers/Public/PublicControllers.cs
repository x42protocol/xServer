using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X42.Feature.API.Requirements;
using X42.Server;

namespace X42.Controllers.Public
{
    /// <inheritdoc />
    /// <summary>
    ///     Controller providing Public Methods for the server.
    /// </summary>
    [Route("")]
    public class PublicController : Controller
    {
        private readonly IX42Server x42Server;

        public PublicController(IX42Server x42Server)
        {
            this.x42Server = x42Server;
        }

        /// <summary>
        ///     Returns a web page to act as a dashboard
        /// </summary>
        /// <returns>text/html content</returns>
        [HttpGet]
        [Route("verion")]
        public IActionResult Verion()
        {
            string content = x42Server.Version.ToString();
            return Content(content);
        }
    }
}