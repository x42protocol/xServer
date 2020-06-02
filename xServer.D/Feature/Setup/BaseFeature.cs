using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using x42.Configuration;
using x42.Server;
using x42.Utilities;

namespace x42.Feature.Setup
{
    /// <summary>
    ///     Base server services, these are the services a server has to have.
    ///     The ConnectionManager feature is also part of the base but may go in a feature of its own.
    ///     The base features are the minimal components required for the xServer system.
    /// </summary>
    public sealed class BaseFeature : ServerFeature
    {
        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        /// <summary>Locations of important folders and files on disk.</summary>
        private readonly DataFolder dataFolder;

        /// <summary>Provider of time functions.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>Logger for the server.</summary>
        private readonly ILogger logger;

        /// <summary>Factory for creating loggers.</summary>
        private readonly ILoggerFactory loggerFactory;

        /// <inheritdoc cref="Network" />
        private readonly XServer network;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime serverLifetime;

        /// <summary>User defined server settings.</summary>
        private readonly ServerSettings serverSettings;

        public BaseFeature(ServerSettings serverSettings,
            DataFolder dataFolder,
            IxServerLifetime serverLifetime,
            IDateTimeProvider dateTimeProvider,
            IAsyncLoopFactory asyncLoopFactory,
            ILoggerFactory loggerFactory,
            XServer network)
        {
            this.serverSettings = Guard.NotNull(serverSettings, nameof(serverSettings));
            this.dataFolder = Guard.NotNull(dataFolder, nameof(dataFolder));
            this.serverLifetime = Guard.NotNull(serverLifetime, nameof(serverLifetime));
            this.network = network;


            this.dateTimeProvider = dateTimeProvider;
            this.asyncLoopFactory = asyncLoopFactory;
            this.loggerFactory = loggerFactory;
            logger = loggerFactory.CreateLogger(GetType().FullName);
        }

        /// <inheritdoc />
        public override async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                // TODO: Run
            });
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            logger.LogInformation("Base disposted");
        }
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="IServerBuilder" />.
    /// </summary>
    public static class ServerBuilderBaseFeatureExtension
    {
        /// <summary>
        ///     Makes the xServer use all the required features - <see cref="BaseFeature" />.
        /// </summary>
        /// <param name="serverBuilder">Builder responsible for creating the server.</param>
        /// <returns>xServer builder's interface to allow fluent code.</returns>
        public static IServerBuilder UseBaseFeature(this IServerBuilder serverBuilder)
        {
            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<BaseFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton(serverBuilder.ServerSettings.LoggerFactory);
                        services.AddSingleton(serverBuilder.ServerSettings.DataFolder);
                        services.AddSingleton<IxServerLifetime, XServerLifetime>();
                        services.AddSingleton<ServerFeatureExecutor>();
                        services.AddSingleton<XServer>().AddSingleton(provider =>
                        {
                            return provider.GetService<XServer>() as IxServer;
                        });
                        services.AddSingleton(DateTimeProvider.Default);
                        services.AddSingleton<IAsyncLoopFactory, AsyncLoopFactory>();

                        // Console
                        services.AddSingleton<IServerStats, ServerStats>();
                    });
            });

            return serverBuilder;
        }
    }
}