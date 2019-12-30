using Microsoft.AspNetCore.Mvc;
using X42.Controllers.Results;
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
        [Route("ping")]
        public IActionResult Ping()
        {
            string content = x42Server.Version.ToString();
            return Content(content);
        }

        /// <summary>
        ///     Registers a servernode to the network.
        /// </summary>
        /// <returns>A <see cref="RegisterResult" /> with registration result.</returns>
        [HttpGet]
        [Route("register")]
        public IActionResult Register()
        {
            RegisterResult model = new RegisterResult
            {
                Success = true
            };

            return Json(model);
        }
    }
}