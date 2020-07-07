using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using x42.Configuration.Logging;
using x42.Feature.Setup;
using x42.Feature.X42Client.Enums;
using x42.ServerNode;
using x42.Server;
using x42.Utilities;

namespace x42.Feature.X42Client
{
    /// <inheritdoc />
    /// <summary>
    ///     Provides an ability to communicate with the X42 Node directly.
    /// </summary>
    public class X42ClientFeature : ServerFeature
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime serverLifetime;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        private X42Node x42Client;

        private readonly X42ClientSettings x42ClientSettings;

        public ConnectionStatus Status => x42Client.Status;
        public ulong BlockTIP => x42Client.BlockTIP;

        public X42ClientFeature(
            ServerNodeBase network,
            ILoggerFactory loggerFactory,
            X42ClientSettings x42ClientSettings,
            IServerStats nodeStats,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory)
        {
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
            this.x42ClientSettings = x42ClientSettings;
            logger = loggerFactory.CreateLogger(GetType().FullName);

            nodeStats.RegisterStats(this.AddComponentStats, StatsType.Component, 1000);
        }

        private void AddComponentStats(StringBuilder builder)
        {
            builder.AppendLine();
            builder.AppendLine($"X42 Node Status: {x42Client.Status}");
        }

        /// <summary>
        ///     Prints command-line help.
        /// </summary>
        /// <param name="serverNodeBase">The servernode to extract values from.</param>
        public static void PrintHelp(ServerNodeBase serverNodeBase)
        {
            X42ClientSettings.PrintHelp(serverNodeBase);
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, ServerNodeBase network)
        {
            X42ClientSettings.BuildDefaultConfigurationFile(builder, network);
        }

        public void Disconnect()
        {
            logger.LogInformation("Disconnected X42 Node");
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            x42Client = new X42Node(x42ClientSettings.Name, x42ClientSettings.Address, x42ClientSettings.Port, logger, serverLifetime, asyncLoopFactory);
            logger.LogInformation("X42 Client Feature Initialized");

            x42Client.StartNodeMonitor();
            logger.LogInformation("X42 Node monitor has started");
            
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Disconnect();
            x42Client.Dispose();
        }

        /// <inheritdoc />
        public override void ValidateDependencies(IServerServiceProvider services)
        {
            // TODO: Check settings and verify features here, then throw exception if not valid
            // Example: throw new ConfigurationException("Something went wrong.");
        }
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="X42ClientExtension" />.
    /// </summary>
    public static class X42ClientExtension
    {
        /// <summary>
        ///     Adds x42 Client components to the server.
        /// </summary>
        /// <param name="serverBuilder">The object used to build the current node.</param>
        /// <returns>The server builder, enriched with the new component.</returns>
        public static IServerBuilder UseX42Client(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<X42ClientFeature>("x42client");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<X42ClientFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<X42ClientFeature>();
                        services.AddSingleton<X42ClientSettings>();
                    });
            });

            return serverBuilder;
        }
    }
}