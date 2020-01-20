using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using X42.Properties;
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

namespace X42.Server
{
    /// <summary>
    ///     Server providing all supported features of the servernode and its network.
    /// </summary>
    public class X42Server : IX42Server
    {
        /// <summary>Instance logger.</summary>
        private ILogger logger;

        /// <summary>Factory for creating loggers.</summary>
        private ILoggerFactory loggerFactory;

        private IAsyncLoop periodicLogLoop;

        /// <summary>Component responsible for starting and stopping all the node's features.</summary>
        private ServerFeatureExecutor serverFeatureExecutor;

        /// <summary>Application life cycle control - triggers when application shuts down.</summary>
        private X42ServerLifetime serverLifetime;

        private readonly ServerSettings nodeSettings;
        private readonly NetworkFeatures network;
        private readonly X42ClientFeature x42FullNode;
        private readonly DatabaseFeatures database;

        /// <summary>Creates new instance of the <see cref="X42Server" />.</summary>
        public X42Server(NetworkFeatures network,
            ServerSettings nodeSettings,
            X42ClientFeature x42FullNode,
            DatabaseFeatures database)
        {
            this.network = network;
            this.nodeSettings = nodeSettings;
            this.x42FullNode = x42FullNode;
            this.database = database;

            State = X42ServerState.Created;
        }

        /// <summary>Server command line and configuration file settings.</summary>
        public ServerSettings Settings { get; private set; }

        /// <summary>Factory for creating and execution of asynchronous loops.</summary>
        public IAsyncLoopFactory AsyncLoopFactory { get; set; }

        /// <summary>Specification of the master node the server runs on.</summary>
        public ServerNodeBase ServerNode { get; internal set; }

        /// <summary>Contains path locations to folders and files on disk.</summary>
        public DataFolder DataFolder { get; set; }

        /// <see cref="IServerStats" />
        private IServerStats ServerStats { get; set; }

        public string LastLogOutput { get; private set; }

        /// <inheritdoc />
        public X42ServerState State { get; private set; }

        /// <inheritdoc />
        public DateTime StartTime { get; set; }

        /// <summary>Provider of date time functionality.</summary>
        public IDateTimeProvider DateTimeProvider { get; set; }

        /// <inheritdoc />
        public IX42ServerLifetime X42ServerLifetime
        {
            get => serverLifetime;
            private set => serverLifetime = (X42ServerLifetime)value;
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
                string versionString = typeof(X42Server).GetTypeInfo().Assembly
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
                        registerResult.FailReason = "Could not verify server";
                    }

                    // TODO: Final Check.
                    // We need to ping server before finalizing. Testing the availability of server will ensure that the server was at one point available.

                    bool serverAdded = network.AddServer(serverNode);
                    if (serverAdded)
                    {
                        registerResult.Success = true;
                    }
                    else
                    {
                        registerResult.FailReason = "Server already exists in repo";
                    }
                }
                else if (serverTier == null || availableTiers.Count() != 1)
                {
                    registerResult.FailReason = "Requested Tier is not available or collateral amount is invalid.";
                }
            }
            else
            {
                if (x42FullNode.Status != ConnectionStatus.Online)
                {
                    registerResult.FailReason = "Node is offline";
                }
                else if (!database.DatabaseConnected)
                {
                    registerResult.FailReason = "Databse is offline";
                }

            }

            return registerResult;
        }

        /// <inheritdoc />
        public void Start()
        {
            State = X42ServerState.Starting;

            if (State == X42ServerState.Disposing || State == X42ServerState.Disposed)
                throw new ObjectDisposedException(nameof(X42Server));

            serverLifetime = Services.ServiceProvider.GetRequiredService<IX42ServerLifetime>() as X42ServerLifetime;
            serverFeatureExecutor = Services.ServiceProvider.GetRequiredService<ServerFeatureExecutor>();

            if (serverLifetime == null)
                throw new InvalidOperationException($"{nameof(IX42ServerLifetime)} must be set.");

            if (serverFeatureExecutor == null)
                throw new InvalidOperationException($"{nameof(ServerFeatureExecutor)} must be set.");

            logger.LogInformation("Starting server.");

            // Initialize all registered features.
            serverFeatureExecutor.Initialize();

            // Fire IServerLifetime.Started.
            serverLifetime.NotifyStarted();

            StartPeriodicLog();

            State = X42ServerState.Started;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (State == X42ServerState.Disposing || State == X42ServerState.Disposed)
                return;

            State = X42ServerState.Disposing;

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

            State = X42ServerState.Disposed;
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
        public X42Server Initialize(ServerServiceProvider serviceProvider)
        {
            State = X42ServerState.Initializing;

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
            logger.LogInformation("x42-Server initialized {0}.", ServerNode.Name);

            State = X42ServerState.Initialized;
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