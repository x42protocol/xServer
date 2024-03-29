﻿using Microsoft.AspNetCore.Mvc;
using x42.Server;

namespace x42.Controllers
{
    /// <inheritdoc />
    /// <summary>
    ///     Controller providing HTML Dashboard
    /// </summary>
    [ApiController]
    [Route("")]
    //[Authorize(Policy = Policy.PrivateAccess)]
    public class DashboardController : Controller
    {
        private readonly IxServer xServer;

        public DashboardController(IxServer xServer)
        {
            this.xServer = xServer;
        }

        /// <summary>
        ///     Returns the last iteration of the log output
        /// </summary>
        /// <returns>text/latest logs</returns>
        [HttpGet]
        [Route("/")]
        public IActionResult Log()
        {
            string content = xServer.LastLogOutput;
            return Content(content);
        }
    }
}