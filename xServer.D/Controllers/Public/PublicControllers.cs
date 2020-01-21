using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using X42.Controllers.Requests;
using X42.Feature.Database.Tables;
using X42.Server;
using X42.Server.Results;

namespace X42.Controllers.Public
{
    /// <inheritdoc />
    /// <summary>
    ///     Controller providing Public Methods for the server.
    /// </summary>
    [ApiController]
    [Route("")]
    public class PublicController : Controller
    {
        private readonly XServer xServer;

        public PublicController(XServer xServer)
        {
            this.xServer = xServer;
        }

        /// <summary>
        ///     Returns a web page to act as a dashboard
        /// </summary>
        /// <returns>text/html content</returns>
        [HttpGet]
        [Route("ping")]
        public IActionResult Ping()
        {
            string content = xServer.Version.ToString();
            return Content(content);
        }

        /// <summary>
        ///     Registers a servernode to the network.
        /// </summary>
        /// <returns>A <see cref="RegisterResult" /> with registration result.</returns>
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest registerRequest)
        {
            ServerNodeData serverNode = new ServerNodeData()
            {
                Name = registerRequest.Name,
                NetworkAddress = registerRequest.NetworkAddress,
                NetworkPort = registerRequest.NetworkPort,
                Signature = registerRequest.Signature,
                PublicAddress = registerRequest.Address,
                Tier = registerRequest.Tier
            };

            RegisterResult registerResult = await xServer.Register(serverNode);

            return Json(registerResult);
        }
    }
}