using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using X42.Controllers.Requests;
using X42.Controllers.Results;
using X42.Feature.Database.Tables;
using X42.Feature.Network;
using X42.Feature.X42Client.Enums;
using X42.Server;
using X42.ServerNode;

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
        private readonly NetworkFeatures network;
        public PublicController(IX42Server x42Server, NetworkFeatures network)
        {
            this.x42Server = x42Server;
            this.network = network;
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
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest registerRequest)
        {
            RegisterResult registerResult = new RegisterResult
            {
                Success = false
            };

            ServerNodeData serverNode = new ServerNodeData()
            {
                Ip = registerRequest.Ip,
                Port = registerRequest.Port,
                Signature = registerRequest.Signature,
                CollateralTX = registerRequest.CollateralTX
            };

            if (network.FullNodeStatus == ConnectionStatus.Online)
            {
                bool serverIsValid = await network.IsServerKeyValid(serverNode);

                if (!serverIsValid)
                {
                    registerResult.FailReason = "Could not verify collateral";
                }

                // Final Check.
                if (serverIsValid)
                {
                    registerResult.Success = true;
                }
            }
            else
            {
                registerResult.FailReason = "Node is offline";
            }

            return Json(registerResult);
        }
    }
}