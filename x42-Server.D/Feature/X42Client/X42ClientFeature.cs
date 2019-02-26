using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using X42.Configuration.Logging;
using X42.Feature.Setup;
using X42.MasterNode;
using X42.Server;

namespace X42.Feature.X42Client
{
    /// <inheritdoc />
    /// <summary>
    ///     Provides an ability to communicate with the X42 Node directly.
    /// </summary>
    public class X42ClientFeature : ServerFeature
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        public X42ClientFeature(MasterNodeBase network, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger(GetType().FullName);
        }

        /// <summary>
        ///     Prints command-line help.
        /// </summary>
        /// <param name="masterNodeBase">The masternode to extract values from.</param>
        public static void PrintHelp(MasterNodeBase masterNodeBase)
        {
            X42ClientSettings.PrintHelp(masterNodeBase);
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, MasterNodeBase network)
        {
            X42ClientSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <summary>
        ///     Connect to the X42 Node.
        /// </summary>
        public void Connect()
        {
            logger.LogInformation("Connected to X42 Node");
        }

        public void Disconnect()
        {
            logger.LogInformation("Disconnected X42 Node");
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            logger.LogInformation("X42 Client Feature Initialized");

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
            // TODO: Check settings and verify features here, then throw exception if not valid
            // Example: throw new ConfigurationException("Something went wrong.");
        }
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="DatabaseFeatures" />.
    /// </summary>
    public static class X42ClientExtension
    {
        /// <summary>
        ///     Adds SQL components to the server.
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