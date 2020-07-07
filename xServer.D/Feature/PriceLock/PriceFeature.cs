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

        /// <summary>Time in seconds between attempts to update my x42/usd price</summary>
        private readonly int updateMyPriceSeconds = 60;

        /// <summary>Time in seconds between attempts to update network x42/usd price</summary>
        private readonly int updateNetworkPriceSeconds = 600;

        public USD Price { get; set; } = new USD();

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
            MonitorPrice();

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


        /// <summary>
        ///     Update my x42/usd price list
        /// </summary>
        private async Task UpdateMyPriceList(CancellationToken cancellationToken)
        {
            // TODO: Add a configuration option for the users to add thier own API to get the prices.

            var coinGeckoPriceResult = await GetCoinGeckoPrice(cancellationToken);
            Price.AddMyPrice(coinGeckoPriceResult.X42Protocol.Usd);
        }

        /// <summary>
        ///     Update the network x42/usd price list
        /// </summary>
        private async Task UpdateNetworkPriceList(CancellationToken cancellationToken)
        {
            var tierThreeServerConnections = GetTierThreeConnectionInfo(Price.NetworkPriceListSize);
            foreach (var serverConnectionInfo in tierThreeServerConnections)
            {
                var priceResult = await GetPriceFromTierThree(cancellationToken, serverConnectionInfo);
                if (priceResult.Price > 0)
                {
                    Price.AddNetworkPrice(priceResult.Price);
                }
            }
        }

        private async Task<PriceResult> GetPriceFromTierThree(CancellationToken cancellationToken, XServerConnectionInfo xServerConnectionInfo)
        {
            PriceResult priceResult = new PriceResult();
            string xServerURL = networkFeatures.GetServerUrl(xServerConnectionInfo.NetworkProtocol, xServerConnectionInfo.NetworkAddress, xServerConnectionInfo.NetworkPort);
            var client = new RestClient(xServerURL);
            var activeServerCountRequest = new RestRequest("/getprice/", Method.GET);
            var getPriceResult = await client.ExecuteAsync<PriceResult>(activeServerCountRequest, cancellationToken).ConfigureAwait(false);
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

        private async Task<CoinGeckoPriceResult> GetCoinGeckoPrice(CancellationToken cancellationToken)
        {
            var client = new RestClient("https://api.coingecko.com/api/v3/simple/");
            var request = new RestRequest("price?ids=x42-protocol&vs_currencies=USD", Method.GET);
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
