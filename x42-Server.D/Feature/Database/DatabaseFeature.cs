using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using X42.Configuration.Logging;
using X42.Feature.Setup;
using X42.MasterNode;
using X42.Server;

namespace X42.Feature.Database
{
    /// <summary>
    ///     Provides an ability to comminicate with diffrent database types.
    /// </summary>
    public class DatabaseFeatures : ServerFeature
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        public DatabaseFeatures(MasterNodeBase network, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger(GetType().FullName);
        }

        /// <summary>
        ///     Prints command-line help.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(MasterNodeBase masterNodeBase)
        {
            DatabaseSettings.PrintHelp(masterNodeBase);
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, MasterNodeBase network)
        {
            DatabaseSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <summary>
        ///     Connect to the database.
        /// </summary>
        /// <param name="walletName">The name of the wallet.</param>
        /// <param name="walletPassword">The password of the wallet.</param>
        public void Connect()
        {
            logger.LogInformation("Connected to database");
        }

        public void Disconnect()
        {
            logger.LogInformation("Disconnected from database");
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            logger.LogInformation("Database Feature Initialized");

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
    public static class DatabaseBuilderExtension
    {
        /// <summary>
        ///     Adds PostgreSQL components to the server.
        /// </summary>
        /// <param name="serverBuilder">The object used to build the current node.</param>
        /// <returns>The server builder, enriched with the new component.</returns>
        public static IServerBuilder UseSql(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<DatabaseFeatures>("database");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<DatabaseFeatures>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<DatabaseFeatures>();
                        services.AddSingleton<DatabaseSettings>();
                    });
            });

            return serverBuilder;
        }

        public static IServerBuilder UseNoql(this IServerBuilder serverBuilder)
        {
            throw new NotImplementedException("MongoDB is not yet supported");
        }
    }
}