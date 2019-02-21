using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;
using X42.Configuration;
using X42.Server;
using X42.Utilities;

namespace X42.Feature.Setup
{
    /// <summary>
    ///     Base server services, these are the services a server has to have.
    ///     The ConnectionManager feature is also part of the base but may go in a feature of its own.
    ///     The base features are the minimal components required to connect to peers and maintain the best chain.
    ///     <para>
    ///         The base server services for a server are:
    ///         <list type="bullet">
    ///             <item>the ConcurrentChain to keep track of the best chain,</item>
    ///             <item>the ConnectionManager to connect with the network,</item>
    ///             <item>DatetimeProvider and Cancellation,</item>
    ///             <item>CancellationProvider and Cancellation,</item>
    ///             <item>DataFolder,</item>
    ///             <item>ChainState.</item>
    ///         </list>
    ///     </para>
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
        private readonly X42Server network;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IX42ServerLifetime serverLifetime;

        /// <summary>User defined server settings.</summary>
        private readonly ServerSettings serverSettings;

        public BaseFeature(ServerSettings serverSettings,
            DataFolder dataFolder,
            IX42ServerLifetime serverLifetime,
            IDateTimeProvider dateTimeProvider,
            IAsyncLoopFactory asyncLoopFactory,
            ILoggerFactory loggerFactory,
            X42Server network)
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
        ///     Makes the x42 server use all the required features - <see cref="BaseFeature" />.
        /// </summary>
        /// <param name="serverBuilder">Builder responsible for creating the server.</param>
        /// <returns>x42 server builder's interface to allow fluent code.</returns>
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
                        services.AddSingleton<IX42ServerLifetime, X42ServerLifetime>();
                        services.AddSingleton<ServerFeatureExecutor>();
                        services.AddSingleton<X42Server>().AddSingleton(provider =>
                        {
                            return provider.GetService<X42Server>() as IX42Server;
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