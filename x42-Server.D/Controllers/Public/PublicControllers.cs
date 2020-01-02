using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using X42.Configuration;
using X42.Controllers.Requests;
using X42.Controllers.Results;
using X42.Feature.Database;
using X42.Feature.Database.Tables;
using X42.Feature.Network;
using X42.Feature.X42Client;
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
        private readonly ServerSettings nodeSettings;
        private readonly IX42Server x42Server;
        private readonly NetworkFeatures network;
        private readonly X42ClientFeature x42FullNode;
        private readonly DatabaseFeatures databaseFeatures;

        public PublicController(
            IX42Server x42Server,
            NetworkFeatures network,
            ServerSettings nodeSettings,
            X42ClientFeature x42FullNode,
            DatabaseFeatures databaseFeatures
            )
        {
            this.x42Server = x42Server;
            this.network = network;
            this.nodeSettings = nodeSettings;
            this.x42FullNode = x42FullNode;
            this.databaseFeatures = databaseFeatures;
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
                Name = registerRequest.Name,
                Ip = registerRequest.Ip,
                Port = registerRequest.Port,
                Signature = registerRequest.Signature,
                TxId = registerRequest.TxId,
                TxOut = registerRequest.TxOut
            };


            // TODO: Refactor this, it doesn't need to be in the contoller and needs to be split up into smaller methods.
            if (x42FullNode.Status == ConnectionStatus.Online && databaseFeatures.DatabaseConnected)
            {
                var collateralDetails = await network.IsTransactionValid(serverNode);
                IEnumerable<Tier> tier = nodeSettings.ServerNode.Tiers.Where(t => t.Collateral.Amount == collateralDetails.collateral);

                if (collateralDetails.isValid && tier.Count() == 1)
                {
                    serverNode.PublicAddress = collateralDetails.publicAddress;

                    bool serverIsValid = await network.IsServerKeyValid(serverNode);

                    if (!serverIsValid)
                    {
                        registerResult.FailReason = "Could not verify server";
                    }

                    // Final Check.
                    if (serverIsValid)
                    {
                        bool serverAdded = network.AddServer(serverNode);
                        if (serverAdded)
                        {
                            registerResult.Success = true;
                        }
                        else
                        {
                            registerResult.FailReason = "Server already exists in repo";
                        }
                    }
                }
                else
                {
                    if (!collateralDetails.isValid)
                    {
                        registerResult.FailReason = "Could not verify collateral";
                    }
                    else if (tier.Count() != 1)
                    {
                        registerResult.FailReason = "Collateral amount is invalid";
                    }
                }
            }
            else
            {
                if (x42FullNode.Status != ConnectionStatus.Online)
                {
                    registerResult.FailReason = "Node is offline";
                }
                else if (!databaseFeatures.DatabaseConnected)
                {
                    registerResult.FailReason = "Databse is offline";
                }

            }

            return Json(registerResult);
        }
    }
}