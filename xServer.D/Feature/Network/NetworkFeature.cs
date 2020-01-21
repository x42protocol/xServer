using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using X42.Configuration.Logging;
using X42.Feature.Database.Context;
using X42.Feature.Database;
using X42.Feature.Setup;
using X42.Feature.X42Client;
using X42.ServerNode;
using X42.Server;
using X42.Utilities;
using X42.Feature.Database.Tables;
using System.Linq;
using X42.Feature.X42Client.RestClient.Responses;
using X42.Configuration;
using X42.Feature.X42Client.Enums;
using Microsoft.EntityFrameworkCore;
using NBitcoin;

namespace X42.Feature.Network
{
    /// <inheritdoc />
    /// <summary>
    ///     Provides an ability to communicate with different network types.
    /// </summary>
    public class NetworkFeatures : ServerFeature
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime serverLifetime;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        private readonly ServerNodeBase network;
        private readonly DatabaseSettings databaseSettings;

        private NetworkMonitor networkMonitor;
        private X42ClientSettings x42ClientSettings;
        private X42Node x42Client;

        public NetworkFeatures(
            ServerNodeBase network,
            ILoggerFactory loggerFactory,
            DatabaseSettings databaseSettings,
            X42ClientSettings x42ClientSettings,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory
            )
        {
            this.network = network;
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.databaseSettings = databaseSettings;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
            this.x42ClientSettings = x42ClientSettings;

            x42Client = new X42Node(x42ClientSettings.Name, x42ClientSettings.Address, x42ClientSettings.Port, logger, serverLifetime, asyncLoopFactory, false);
        }

        /// <summary>
        ///     Prints command-line help.
        /// </summary>
        /// <param name="serverNodeBase">The servernode to extract values from.</param>
        public static void PrintHelp(ServerNodeBase serverNodeBase)
        {
            NetworkSettings.PrintHelp(serverNodeBase);
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, ServerNodeBase network)
        {
            NetworkSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <summary>
        ///     Connect to the network.
        /// </summary>
        public void Connect()
        {
            logger.LogInformation("Connecting to network");
        }

        public void Disconnect()
        {
            logger.LogInformation("Disconnected from network");
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            networkMonitor = new NetworkMonitor(logger, serverLifetime, asyncLoopFactory, databaseSettings, this);

            networkMonitor.Start();

            logger.LogInformation("Network Initialized");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Disconnect();
        }

        /// <inheritdoc />
        public override void ValidateDependencies(IServerServiceProvider services)
        {
            if (string.IsNullOrEmpty(x42ClientSettings.Name))
            {
                throw new ConfigurationException("x42Client Name setting must be set.");
            }

            if (x42ClientSettings.Port <= 0)
            {
                throw new ConfigurationException("x42Client Port setting must be set.");
            }

            if (x42ClientSettings.Address.AddressFamily == System.Net.Sockets.AddressFamily.Unknown)
            {
                throw new ConfigurationException("x42Client Address setting must be set, and a valid IP address.");
            }
        }

        public async Task<bool> IsServerKeyValid(ServerNodeData serverNode)
        {
            string serverKey = $"{serverNode.Name}{serverNode.NetworkAddress}{serverNode.NetworkPort}";

            return await x42Client.VerifyMessageAsync(serverNode.PublicAddress, serverKey, serverNode.Signature);
        }

        public async Task<Money> GetServerCollateral(ServerNodeData serverNode)
        {
            GetAddressesBalancesResponse addressBalance = await x42Client.GetAddressBalances(serverNode.PublicAddress);

            if (addressBalance.balances.Count() == 1 && addressBalance.balances.FirstOrDefault().address == serverNode.PublicAddress)
            {
                return Money.FromUnit(addressBalance.balances.FirstOrDefault().balance, MoneyUnit.Satoshi);
            }

            return Money.Zero;
        }

        /// <summary>
        ///     Add a server to the repo
        /// </summary>
        /// <param name="serverNodeData">Server Node Data.</param>
        /// <returns>Will return true if new record wad created, otherwise false.</returns>
        public bool AddServer(ServerNodeData serverNodeData)
        {
            bool result = false;

            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<ServerNodeData> serverNodes = dbContext.ServerNodes.Where(s => s.Signature == serverNodeData.Signature);
                if (serverNodes.Count() == 0)
                {
                    var newRecord = dbContext.Add(serverNodeData);
                    if (newRecord.State == EntityState.Added)
                    {
                        dbContext.SaveChanges();
                        result = true;
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="NetworkFeatures" />.
    /// </summary>
    public static class NetworkBuilderExtension
    {
        /// <summary>
        ///     Adds SQL components to the server.
        /// </summary>
        /// <param name="serverBuilder">The object used to build the current node.</param>
        /// <returns>The server builder, enriched with the new component.</returns>
        public static IServerBuilder UseNetwork(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<NetworkFeatures>("network");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<NetworkFeatures>()
                    .DependOn<DatabaseFeatures>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<NetworkFeatures>();
                        services.AddSingleton<NetworkSettings>();
                    });
            });

            return serverBuilder;
        }
    }
}