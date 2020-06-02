using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using x42.Controllers.Results;
using x42.Controllers.Requests;
using x42.Feature.Database.Tables;
using x42.Server;
using x42.Server.Results;

namespace x42.Controllers.Public
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
        ///     Returns simple information about the xServer.
        /// </summary>
        /// <returns>A JSON object containing the xServer information.</returns>
        [HttpGet]
        [Route("ping")]
        public IActionResult Ping()
        {
            PingResult pingResult = new PingResult()
            {
                Version = xServer.Version.ToString(),
                BestBlockHeight = xServer.BestBlockHeight
            };
            return Json(pingResult);
        }

        /// <summary>
        ///     Returns the top xServers available.
        /// </summary>
        /// <param name="top">The number of top xServers to return.</param>
        /// <returns>A JSON object containing the top xServers available.</returns>
        [HttpGet]
        [Route("gettop")]
        public IActionResult GetTop([FromQuery] int top = 10)
        {
            TopResult topResult = xServer.GetTopXServers(top);
            return Json(topResult);
        }

        /// <summary>
        ///     Registers a servernode to the network.
        /// </summary>
        /// <param name="registerRequest">The object with all of the nessesary data to register a xServer.</param>
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