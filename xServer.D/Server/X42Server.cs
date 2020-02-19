using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using X42.Configuration;
using X42.Feature.Setup;
using X42.ServerNode;
using X42.Utilities;
using X42.Server.Results;
using X42.Feature.Database.Tables;
using X42.Feature.Network;
using X42.Feature.X42Client;
using X42.Feature.Database;
using X42.Feature.X42Client.Enums;
using System.Collections.Generic;
using x42.Properties;

namespace X42.Server
{
    /// <summary>
    ///     Server providing all supported features of the servernode and its network.
    /// </summary>
    public class XServer : IxServer
    {
        /// <summary>Instance logger.</summary>
        private ILogger logger;

        /// <summary>Factory for creating loggers.</summary>
        private ILoggerFactory loggerFactory;

        private IAsyncLoop periodicLogLoop;

        /// <summary>Component responsible for starting and stopping all the node's features.</summary>
        private ServerFeatureExecutor serverFeatureExecutor;

        /// <summary>Application life cycle control - triggers when application shuts down.</summary>
        private XServerLifetime serverLifetime;

        private readonly ServerSettings nodeSettings;
        private readonly NetworkFeatures network;
        private readonly X42ClientFeature x42FullNode;
        private readonly DatabaseFeatures database;

        /// <summary>Creates new instance of the <see cref="XServer" />.</summary>
        public XServer(NetworkFeatures network,
            ServerSettings nodeSettings,
            X42ClientFeature x42FullNode,
            DatabaseFeatures database)
        {
            this.network = network;
            this.nodeSettings = nodeSettings;
            this.x42FullNode = x42FullNode;
            this.database = database;

            State = XServerState.Created;
        }

        /// <summary>Server command line and configuration file settings.</summary>
        public ServerSettings Settings { get; private set; }

        /// <summary>Factory for creating and execution of asynchronous loops.</summary>
        public IAsyncLoopFactory AsyncLoopFactory { get; set; }

        /// <summary>Specification of the xServer.</summary>
        public ServerNodeBase ServerNode { get; internal set; }

        /// <summary>Contains path locations to folders and files on disk.</summary>
        public DataFolder DataFolder { get; set; }

        /// <see cref="IServerStats" />
        private IServerStats ServerStats { get; set; }

        public string LastLogOutput { get; private set; }

        /// <inheritdoc />
        public XServerState State { get; private set; }

        /// <inheritdoc />
        public DateTime StartTime { get; set; }

        /// <summary>Provider of date time functionality.</summary>
        public IDateTimeProvider DateTimeProvider { get; set; }

        /// <inheritdoc />
        public IxServerLifetime xServerLifetime
        {
            get => serverLifetime;
            private set => serverLifetime = (XServerLifetime)value;
        }


        /// <inheritdoc />
        public IServerServiceProvider Services { get; set; }

        public T ServerService<T>(bool failWithDefault = false)
        {
            if (Services != null && Services.ServiceProvider != null)
            {
                T service = Services.ServiceProvider.GetService<T>();
                if (service != null)
                    return service;
            }

            if (failWithDefault)
                return default;

            throw new InvalidOperationException($"The {typeof(T)} service is not supported");
        }


        /// <inheritdoc />
        public Version Version
        {
            get
            {
                string versionString = typeof(XServer).GetTypeInfo().Assembly
                                           .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ??
                                       PlatformServices.Default.Application.ApplicationVersion;

                if (!string.IsNullOrEmpty(versionString))
                    try
                    {
                        return new Version(versionString);
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (OverflowException)
                    {
                    }

                return new Version(0, 0);
            }
        }

        private bool IsServerReady()
        {
            return x42FullNode.Status == ConnectionStatus.Online && database.DatabaseConnected;
        }

        public ulong BestBlockHeight { get => x42FullNode.BlockTIP; }

        public async Task<RegisterResult> Register(ServerNodeData serverNode)
        {
            RegisterResult registerResult = new RegisterResult
            {
                Success = false
            };

            if (IsServerReady())
            {
                var collateral = await network.GetServerCollateral(serverNode);
                IEnumerable<Tier> availableTiers = nodeSettings.ServerNode.Tiers.Where(t => t.Collateral.Amount <= collateral);
                Tier serverTier = availableTiers.Where(t => t.Level == (Tier.TierLevel)serverNode.Tier).FirstOrDefault();

                if (serverTier != null)
                {
                    bool serverIsValid = await network.IsServerKeyValid(serverNode);

                    if (!serverIsValid)
                    {
                        registerResult.ResultMessage = "Could not verify server";
                    }

                    // TODO: Final Check.
                    // We need to ping server before finalizing. Testing the availability of server will ensure that the server was at one point available.

                    bool serverAdded = network.AddServer(serverNode);
                    if (!serverAdded)
                    {
                        registerResult.ResultMessage = "Server already exists in repo";
                    }
                    registerResult.Success = true;

                }
                else if (serverTier == null || availableTiers.Count() != 1)
                {
                    registerResult.ResultMessage = "Requested Tier is not available or collateral amount is invalid.";
                }
            }
            else
            {
                if (x42FullNode.Status != ConnectionStatus.Online)
                {
                    registerResult.ResultMessage = "Node is offline";
                }
                else if (!database.DatabaseConnected)
                {
                    registerResult.ResultMessage = "Databse is offline";
                }

            }

            return registerResult;
        }

        /// <inheritdoc />
        public void Start()
        {
            State = XServerState.Starting;

            if (State == XServerState.Disposing || State == XServerState.Disposed)
                throw new ObjectDisposedException(nameof(XServer));

            serverLifetime = Services.ServiceProvider.GetRequiredService<IxServerLifetime>() as XServerLifetime;
            serverFeatureExecutor = Services.ServiceProvider.GetRequiredService<ServerFeatureExecutor>();

            if (serverLifetime == null)
                throw new InvalidOperationException($"{nameof(IxServerLifetime)} must be set.");

            if (serverFeatureExecutor == null)
                throw new InvalidOperationException($"{nameof(ServerFeatureExecutor)} must be set.");

            logger.LogInformation("Starting server.");

            // Initialize all registered features.
            serverFeatureExecutor.Initialize();

            // Fire IServerLifetime.Started.
            serverLifetime.NotifyStarted();

            StartPeriodicLog();

            State = XServerState.Started;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (State == XServerState.Disposing || State == XServerState.Disposed)
                return;

            State = XServerState.Disposing;

            logger.LogInformation("Closing server pending.");

            // Fire IServerLifetime.Stopping.
            serverLifetime.StopApplication();

            logger.LogInformation("Disposing periodic logging loops.");
            periodicLogLoop?.Dispose();

            logger.LogInformation("Disposing settings.");
            Settings.Dispose();

            // Fire IServerLifetime.Stopped.
            logger.LogInformation("Notify application has stopped.");
            serverLifetime.NotifyStopped();

            State = XServerState.Disposed;
        }

        public T ServerFeature<T>(bool failWithError = false)
        {
            if (Services != null)
            {
                T feature = Services.Features.OfType<T>().FirstOrDefault();
                if (feature != null)
                    return feature;
            }

            if (!failWithError)
                return default;

            throw new InvalidOperationException($"The {typeof(T)} feature is not supported");
        }

        /// <inheritdoc />
        public XServer Initialize(ServerServiceProvider serviceProvider)
        {
            State = XServerState.Initializing;

            Guard.NotNull(serviceProvider, nameof(serviceProvider));

            Services = serviceProvider;

            logger = Services.ServiceProvider.GetService<ILoggerFactory>().CreateLogger(GetType().FullName);

            DataFolder = Services.ServiceProvider.GetService<DataFolder>();
            DateTimeProvider = Services.ServiceProvider.GetService<IDateTimeProvider>();
            ServerNode = Services.ServiceProvider.GetService<ServerNodeBase>();
            Settings = Services.ServiceProvider.GetService<ServerSettings>();
            ServerStats = Services.ServiceProvider.GetService<IServerStats>();

            loggerFactory = Services.ServiceProvider.GetService<ServerSettings>().LoggerFactory;

            AsyncLoopFactory = Services.ServiceProvider.GetService<IAsyncLoopFactory>();

            logger.LogInformation(Resources.AsciiLogo);
            logger.LogInformation("xServer initialized {0}.", ServerNode.Name);

            State = XServerState.Initialized;
            StartTime = DateTimeProvider.GetUtcNow();
            return this;
        }

        /// <summary>
        ///     Starts a loop to periodically log statistics about server's status very couple of seconds.
        ///     <para>
        ///         These logs are also displayed on the console.
        ///     </para>
        /// </summary>
        private void StartPeriodicLog()
        {
            periodicLogLoop = AsyncLoopFactory.Run("PeriodicLog", cancellation =>
                {
                    string stats = ServerStats.GetStats();

                    logger.LogInformation(stats);
                    LastLogOutput = stats;

                    return Task.CompletedTask;
                },
                serverLifetime.ApplicationStopping,
                TimeSpans.FiveSeconds,
                TimeSpans.FiveSeconds);
        }
    }
}