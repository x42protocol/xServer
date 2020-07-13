using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using x42.Configuration.Logging;
using x42.Feature.Database;
using x42.Feature.Setup;
using x42.ServerNode;
using x42.Server;
using x42.Utilities;
using System.Collections.Generic;
using x42.Feature.Database.Context;
using System.Linq;
using x42.Controllers.Results;
using x42.Feature.Database.Tables;
using System;
using System.Threading;
using RestSharp;
using x42.Feature.Network;
using System.Net;
using x42.Feature.PriceLock.Results;
using x42.Controllers.Requests;
using Microsoft.EntityFrameworkCore;

namespace x42.Feature.PriceLock
{
    /// <inheritdoc />
    /// <summary>
    ///     Provides the ability to agree on a price based on supplied pairs.
    /// </summary>
    public class PriceFeature : ServerFeature
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        ///     A cancellation token source that can cancel the node monitoring processes and is linked to the <see cref="IxServerLifetime.ApplicationStopping"/>.
        /// </summary>
        private CancellationTokenSource networkCancellationTokenSource;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime serverLifetime;

        /// <summary>Loop in which the node attempts to maintain an avrage agreed price accross the network.</summary>
        private IAsyncLoop networkPriceLoop;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        private readonly ServerNodeBase network;
        private readonly DatabaseSettings databaseSettings;
        private readonly DatabaseFeatures database;
        private readonly NetworkFeatures networkFeatures;

        /// <summary>Time in seconds between attempts to update my x42/pair price</summary>
        private readonly int updateMyPriceSeconds = 60;

        /// <summary>Time in seconds between attempts to update network x42/pair price</summary>
        private readonly int updateNetworkPriceSeconds = 600;

        /// <summary>Fee for the price lock. Default is 1%. TODO: Add this into configuration.</summary>
        private readonly decimal priceLockFeePercent = 1;

        public List<FiatPair> FiatPairs { get; set; } = new List<FiatPair>();
        private PriceLockValidation priceLockValidation;

        public PriceFeature(
            ServerNodeBase network,
            ILoggerFactory loggerFactory,
            DatabaseSettings databaseSettings,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory,
            DatabaseFeatures database,
            NetworkFeatures networkFeatures
            )
        {
            this.network = network;
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.databaseSettings = databaseSettings;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
            this.database = database;
            this.networkFeatures = networkFeatures;
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            var pairs = EnumUtil.GetValues<FiatCurrency>();
            foreach (var pair in pairs)
            {
                FiatPairs.Add(new FiatPair(pair));
            }

            MonitorPrice();
            priceLockValidation = new PriceLockValidation(networkFeatures);

            logger.LogInformation("Price Initialized");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void ValidateDependencies(IServerServiceProvider services)
        {
        }

        private void MonitorPrice()
        {
            this.networkCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { serverLifetime.ApplicationStopping });

            this.networkPriceLoop = asyncLoopFactory.Run("Price.MyMonitor", async token =>
            {
                try
                {
                    await UpdateMyPriceList(this.networkCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_MY_PRICE]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromSeconds(this.updateMyPriceSeconds),
            startAfter: TimeSpans.Second);

            this.networkPriceLoop = asyncLoopFactory.Run("Price.NetworkMonitor", async token =>
            {
                try
                {
                    await UpdateNetworkPriceList(this.networkCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_NETWORK_PRICE]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromSeconds(this.updateNetworkPriceSeconds),
            startAfter: TimeSpans.Second);
        }

        public async Task<PriceLockResult> CreatePriceLock(CreatePriceLockRequest priceLockRequest)
        {
            var result = new PriceLockResult();
            var fiatPair = FiatPairs.Where(f => (int)f.Currency == priceLockRequest.RequestAmountPair).FirstOrDefault();
            if (fiatPair != null)
            {
                var price = Math.Round(priceLockRequest.RequestAmount / fiatPair.GetPrice(), 8);
                var fee = Math.Round(price * priceLockFeePercent / 100, 8);
                var feeAddress = networkFeatures.GetMyKeyAddress();

                using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
                {
                    var newPriceLock = new PriceLockData()
                    {
                        DestinationAddress = priceLockRequest.DestinationAddress,
                        DestinationAmount = price,
                        FeeAmount = fee,
                        FeeAddress = feeAddress,
                        ExpireBlock = priceLockRequest.ExpireBlock,
                        RequestAmount = priceLockRequest.RequestAmount,
                        RequestAmountPair = priceLockRequest.RequestAmountPair,
                        Status = (int)Status.New
                    };
                    var newPriceLockRecord = dbContext.Add(newPriceLock);
                    if (newPriceLockRecord.State == EntityState.Added)
                    {
                        string signature = await networkFeatures.SignPriceLock($"{newPriceLock.PriceLockId}{newPriceLock.DestinationAddress}{newPriceLock.DestinationAmount}{newPriceLock.FeeAddress}{newPriceLock.FeeAmount}");
                        if (!string.IsNullOrEmpty(signature))
                        {
                            newPriceLock.PriceLockSignature = signature;
                            dbContext.SaveChanges();

                            result.DestinationAddress = newPriceLock.DestinationAddress;
                            result.DestinationAmount = newPriceLock.DestinationAmount;
                            result.FeeAddress = newPriceLock.FeeAddress;
                            result.FeeAmount = newPriceLock.FeeAmount;
                            result.RequestAmount = newPriceLock.RequestAmount;
                            result.RequestAmountPair = newPriceLock.RequestAmountPair;
                            result.PriceLockId = newPriceLock.PriceLockId.ToString();
                            result.PriceLockSignature = newPriceLock.PriceLockSignature;
                            result.Status = (int)Status.New;
                            result.Success = true;
                        }
                        else
                        {
                            result.ResultMessage = "Problem with node, Failed to sign price lock.";
                            result.Success = false;
                        }
                    }
                }
            }
            else
            {
                result.ResultMessage = "The pair supplied does not exist.";
                result.Success = false;
            }
            return result;
        }

        public async Task<ValidatePriceLockPayeeResult> ValidatePriceLockPayee(string rawHex, string pricelockId, string signature)
        {
            var result = new ValidatePriceLockPayeeResult();

            var paymentTransaction = await networkFeatures.DecodeRawTransaction(rawHex);

            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {

            }
            return result;
        }

        /// <summary>
        ///     Update my x42/pair price list
        /// </summary>
        private async Task UpdateMyPriceList(CancellationToken cancellationToken)
        {
            // TODO: Add a configuration option for the users to add thier own API to get the prices.
            foreach (var fiatPair in FiatPairs)
            {
                var coinGeckoPriceResult = await GetCoinGeckoPrice(cancellationToken, fiatPair.Currency);
                fiatPair.AddMyPrice(coinGeckoPriceResult.X42Protocol.Price);
            }
        }

        /// <summary>
        ///     Update the network x42/pair price list
        /// </summary>
        private async Task UpdateNetworkPriceList(CancellationToken cancellationToken)
        {
            var networkPriceListSize = FiatPairs.FirstOrDefault().NetworkPriceListSize;
            var tierThreeServerConnections = GetTierThreeConnectionInfo(networkPriceListSize);
            foreach (var serverConnectionInfo in tierThreeServerConnections)
            {
                var nodePriceResults = await GetPriceFromTierThree(cancellationToken, serverConnectionInfo);
                foreach (var nodeResult in nodePriceResults)
                {
                    if (nodeResult.Price > 0)
                    {
                        var fiatPair = FiatPairs.Where(f => (int)f.Currency == nodeResult.Pair).FirstOrDefault();
                        if (fiatPair != null)
                        {
                            fiatPair.AddNetworkPrice(nodeResult.Price);
                        }
                    }
                }
            }
        }

        private async Task<List<PriceResult>> GetPriceFromTierThree(CancellationToken cancellationToken, XServerConnectionInfo xServerConnectionInfo)
        {
            List<PriceResult> priceResult = new List<PriceResult>();
            string xServerURL = networkFeatures.GetServerUrl(xServerConnectionInfo.NetworkProtocol, xServerConnectionInfo.NetworkAddress, xServerConnectionInfo.NetworkPort);
            var client = new RestClient(xServerURL);
            var activeServerCountRequest = new RestRequest("/getprices/", Method.GET);
            var getPriceResult = await client.ExecuteAsync<List<PriceResult>>(activeServerCountRequest, cancellationToken).ConfigureAwait(false);
            if (getPriceResult.StatusCode == HttpStatusCode.OK)
            {
                priceResult = getPriceResult.Data;
            }
            return priceResult;
        }

        private List<XServerConnectionInfo> GetTierThreeConnectionInfo(int takeTop)
        {
            var tierThreeAddresses = new List<XServerConnectionInfo>();

            // Remove any servers that have been unavailable past the grace period.
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                var tierThreeServers = dbContext.ServerNodes.Where(s => s.Tier == (int)Tier.TierLevel.Three && s.Active).OrderBy(s => s.Priority).Take(takeTop);
                foreach (ServerNodeData server in tierThreeServers)
                {
                    var xServerConnectionInfo = new XServerConnectionInfo()
                    {
                        NetworkAddress = server.NetworkAddress,
                        NetworkProtocol = server.NetworkProtocol,
                        NetworkPort = server.NetworkPort,
                        Priotiry = server.Priority
                    };
                    tierThreeAddresses.Add(xServerConnectionInfo);
                }
                dbContext.SaveChanges();
            }
            return tierThreeAddresses;
        }

        private async Task<CoinGeckoPriceResult> GetCoinGeckoPrice(CancellationToken cancellationToken, FiatCurrency currency)
        {
            var client = new RestClient("https://api.coingecko.com/api/v3/simple/");
            var request = new RestRequest($"price?ids=x42-protocol&vs_currencies={currency}", Method.GET);
            var response = await client.ExecuteAsync<CoinGeckoPriceResult>(request, cancellationToken);
            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response from CoinGecko. Check inner details for more info.";
                throw new Exception(message, response.ErrorException);
            }
            return response.Data;
        }
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="PriceFeature" />.
    /// </summary>
    public static class PriceBuilderExtension
    {
        /// <summary>
        ///     Adds Price feature
        /// </summary>
        /// <param name="serverBuilder">The object used to build the current node.</param>
        /// <returns>The server builder, enriched with the new component.</returns>
        public static IServerBuilder UsePriceLock(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<PriceFeature>("price");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<PriceFeature>()
                    .DependOn<DatabaseFeatures>()
                    .DependOn<NetworkFeatures>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<PriceFeature>();
                    });
            });

            return serverBuilder;
        }
    }
}
