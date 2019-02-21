using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using X42.Configuration.Logging;
using X42.Feature.Setup;
using X42.MasterNode;
using X42.Server;

namespace X42.Feature.FullNode
{
    /// <summary>
    ///     Provides the Full Node layer.
    /// </summary>
    public class FullNodeFeature : ServerFeature
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        public FullNodeFeature(MasterNodeBase network, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger(GetType().FullName);
        }

        /// <summary>
        ///     Prints command-line help.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(MasterNodeBase masterNodeBase)
        {
            FullNodeSettings.PrintHelp(masterNodeBase);
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, MasterNodeBase network)
        {
            FullNodeSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <summary>
        ///     Connect to the full node.
        /// </summary>
        /// <param name="walletName">The name of the wallet.</param>
        /// <param name="walletPassword">The password of the wallet.</param>
        public void Connect()
        {
            logger.LogInformation("Connected to full node");
        }

        public void Disconnect()
        {
            logger.LogInformation("Disconnected from full node");
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            logger.LogInformation("Full Node Feature Initialized");

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
            // TODO: Check settings and verify features here, then throw exteption if not valid
            // Example: throw new ConfigurationException("Somethign went wrong.");
        }
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="IFullNodeBuilder" />.
    /// </summary>
    public static class FullNodeBuilderExtension
    {
        /// <summary>
        ///     Adds POW and POS miner components to the node, so that it can mine or stake.
        /// </summary>
        /// <param name="fullNodeBuilder">The object used to build the current node.</param>
        /// <returns>The full node builder, enriched with the new component.</returns>
        public static IServerBuilder UseFullNode(this IServerBuilder fullNodeBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<FullNodeFeature>("fullnode");

            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<FullNodeFeature>()
                    .FeatureServices(services => { services.AddSingleton<FullNodeSettings>(); });
            });

            return fullNodeBuilder;
        }
    }
}