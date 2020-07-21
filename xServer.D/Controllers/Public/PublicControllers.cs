using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using x42.Controllers.Results;
using x42.Controllers.Requests;
using x42.Feature.Database.Tables;
using x42.Server;
using x42.Server.Results;
using x42.Feature.Profile;
using x42.Utilities.JsonErrors;
using System.Net;
using x42.Feature.PriceLock;
using System.Linq;
using System.Collections.Generic;
using System;

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
        private readonly ProfileFeature profileFeature;
        private readonly PriceFeature priceFeature;

        public PublicController(XServer xServer, ProfileFeature profileFeature, PriceFeature priceFeature)
        {
            this.xServer = xServer;
            this.profileFeature = profileFeature;
            this.priceFeature = priceFeature;
        }

        /// <summary>
        ///     Returns simple information about the xServer.
        /// </summary>
        /// <returns>A JSON object containing the xServer information.</returns>
        [HttpGet]
        [Route("ping")]
        public IActionResult Ping()
        {
            xServer.Stats.IncrementPublicRequest();
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
            xServer.Stats.IncrementPublicRequest();
            TopResult topResult = xServer.GetTopXServers(top);
            return Json(topResult);
        }

        /// <summary>
        ///     Registers a servernode to the network.
        /// </summary>
        /// <param name="registerRequest">The object with all of the nessesary data to register a xServer.</param>
        /// <returns>A <see cref="RegisterResult" /> with registration result.</returns>
        [HttpPost]
        [Route("registerserver")]
        public async Task<IActionResult> RegisterServerAsync([FromBody] ServerRegisterRequest registerRequest)
        {
            xServer.Stats.IncrementPublicRequest();
            ServerNodeData serverNode = new ServerNodeData()
            {
                ProfileName = registerRequest.ProfileName,
                NetworkAddress = registerRequest.NetworkAddress,
                NetworkPort = registerRequest.NetworkPort,
                KeyAddress = registerRequest.KeyAddress,
                SignAddress = registerRequest.SignAddress,
                FeeAddress = registerRequest.FeeAddress,
                Signature = registerRequest.Signature,
                Tier = registerRequest.Tier,
                NetworkProtocol = registerRequest.NetworkProtocol
            };

            RegisterResult registerResult = await xServer.Register(serverNode);
            return Json(registerResult);
        }

        /// <summary>
        ///     Returns the active xServer count.
        /// </summary>
        /// <returns>A JSON object containing a count of active xServers.</returns>
        [HttpGet]
        [Route("getactivecount")]
        public IActionResult GetActiveCount()
        {
            xServer.Stats.IncrementPublicRequest();
            CountResult topResult = new CountResult()
            {
                Count = xServer.GetActiveServerCount()
            };
            return Json(topResult);
        }

        /// <summary>
        ///     Returns the active xServers.
        /// </summary>
        /// <returns>A JSON object containing a list of active xServers.</returns>
        [HttpGet]
        [Route("getallactivexservers")]
        public IActionResult GetAllActiveXServers()
        {
            xServer.Stats.IncrementPublicRequest();
            var allServers = xServer.GetAllActiveXServers();
            return Json(allServers);
        }

        /// <summary>
        ///     Will lookup the profile, and return the profile data.
        /// </summary>
        /// <returns>A JSON object containing the profile requested.</returns>
        [HttpGet]
        [Route("getprofile")]
        public IActionResult GetProfile(string name = "", string keyAddress = "")
        {
            xServer.Stats.IncrementPublicRequest();
            if (xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Two)
            {
                var profileRequest = new ProfileRequest()
                {
                    Name = name,
                    KeyAddress = keyAddress
                };
                var profile = profileFeature.GetProfile(profileRequest);
                return Json(profile);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 2 requirement not meet", "The node you requested is not a tier 2 node.");
            }
        }

        /// <summary>
        ///     Reserves a profile to the network.
        /// </summary>
        /// <param name="reserveRequest">The object with all of the nessesary data to reserve a profile.</param>
        /// <returns>A <see cref="ReserveProfileResult" /> with reservation result.</returns>
        [HttpPost]
        [Route("reserveprofile")]
        public async Task<IActionResult> ReserveProfile([FromBody] ProfileReserveRequest reserveRequest)
        {
            xServer.Stats.IncrementPublicRequest();
            if (xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Two)
            {
                var reserveResult = await profileFeature.ReserveProfile(reserveRequest);
                return Json(reserveResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 2 requirement not meet", "The node you requested is not a tier 2 node.");
            }
        }

        /// <summary>
        ///     Used for syncing the profile reservations.
        /// </summary>
        /// <param name="reserveRequest">The object with all of the nessesary data to sync a profile reservation.</param>
        /// <returns>A <see cref="bool" /> with reservation result.</returns>
        [HttpPost]
        [Route("syncprofilereservation")]
        public async Task<IActionResult> SyncProfileReservation([FromBody] ProfileReserveSyncRequest profileReserveSyncRequest)
        {
            xServer.Stats.IncrementPublicRequest();
            if (xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Two)
            {
                var reserveResult = await profileFeature.SyncProfileReservation(profileReserveSyncRequest);
                return Json(reserveResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 2 requirement not meet", "The node you requested is not a tier 2 node.");
            }
        }

        /// <summary>
        ///     Get my average price.
        /// </summary>
        /// <returns>A JSON object containing price information.</returns>
        [HttpGet]
        [Route("getprice")]
        public IActionResult GetPrice(int fiatPairId)
        {
            xServer.Stats.IncrementPublicRequest();
            if (xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var fiatPair = priceFeature.FiatPairs.Where(f => (int)f.Currency == fiatPairId).FirstOrDefault();
                if (fiatPair != null)
                {
                    PriceResult priceResult = new PriceResult()
                    {
                        Price = fiatPair.GetMytPrice()
                    };
                    return Json(priceResult);
                }
                else
                {
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Invalid Pair", "The pair supplied does not exist.");
                }
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Get my average price list.
        /// </summary>
        /// <returns>A JSON object containing price information.</returns>
        [HttpGet]
        [Route("getprices")]
        public IActionResult GetPrices()
        {
            xServer.Stats.IncrementPublicRequest();
            if (xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var priceResults = new List<PriceResult>();
                foreach (var pair in priceFeature.FiatPairs)
                {
                    PriceResult priceResult = new PriceResult()
                    {
                        Price = pair.GetMytPrice(),
                        Pair = (int)pair.Currency
                    };
                    priceResults.Add(priceResult);
                }
                return Json(priceResults);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Create a price lock.
        /// </summary>
        /// <param name="priceLockRequest">The object with all of the nessesary data to create a price lock.</param>
        /// <returns>A <see cref="PriceLockResult" /> with price lock results.</returns>
        [HttpPost]
        [Route("createpricelock")]
        public async Task<IActionResult> CreatePriceLock([FromBody] CreatePriceLockRequest priceLockRequest)
        {
            xServer.Stats.IncrementPublicRequest();
            if (xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var priceLockResult = await priceFeature.CreatePriceLock(priceLockRequest);
                return Json(priceLockResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Update a price lock.
        /// </summary>
        /// <param name="priceLockData">The object with all of the nessesary data to update a price lock.</param>
        /// <returns>A <see cref="bool" /> with the result.</returns>
        [HttpPost]
        [Route("updatepricelock")]
        public async Task<IActionResult> UpdatePriceLock([FromBody] PriceLockResult priceLockData)
        {
            xServer.Stats.IncrementPublicRequest();
            if (xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var priceLockResult = await priceFeature.UpdatePriceLock(priceLockData);
                return Json(priceLockResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Get available price lock pairs
        /// </summary>
        /// <returns>A list with all of the available pairs for a price lock.</returns>
        [HttpGet]
        [Route("getavailablepairs")]
        public IActionResult GetAvailablePairs()
        {
            xServer.Stats.IncrementPublicRequest();
            if (xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var pairList = priceFeature.GetPairList();
                return Json(pairList);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Get a price lock.
        /// </summary>
        /// <param name="priceLockId">The ID of the price lock.</param>
        /// <returns>A <see cref="PriceLockResult" /> with price lock information.</returns>
        [HttpGet]
        [Route("getpricelock")]
        public IActionResult GetPriceLock(string priceLockId)
        {
            xServer.Stats.IncrementPublicRequest();
            if (xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                if (Guid.TryParse(priceLockId, out Guid validPriceLockId))
                {
                    var priceLockResult = priceFeature.GetPriceLock(validPriceLockId);
                    return Json(priceLockResult);
                }
                else
                {
                    return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Invalid pricelock id", "The price lock id is not a valid Guid");
                }
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

        /// <summary>
        ///     Submit the payment for a price lock.
        /// </summary>
        /// <param name="submitPaymentRequest">The object with all of the nessesary data to submit payment.</param>
        /// <returns>A <see cref="SubmitPaymentResult" /> with submission results.</returns>
        [HttpPost]
        [Route("submitpayment")]
        public async Task<IActionResult> SubmitPayment([FromBody] SubmitPaymentRequest submitPaymentRequest)
        {
            xServer.Stats.IncrementPublicRequest();
            if (xServer.Stats.TierLevel == ServerNode.Tier.TierLevel.Three)
            {
                var submitPaymentResult = await priceFeature.SubmitPayment(submitPaymentRequest);
                return Json(submitPaymentResult);
            }
            else
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Tier 3 requirement not meet", "The node you requested is not a tier 3 node.");
            }
        }

    }
}