using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using x42.Configuration;
using x42.Feature.Setup;
using x42.ServerNode;
using x42.Utilities;
using x42.Server.Results;
using x42.Feature.Database.Tables;
using x42.Feature.Network;
using x42.Feature.X42Client;
using x42.Feature.Database;
using System.Collections.Generic;
using x42.Properties;
using x42.Controllers.Requests;
using x42.Controllers.Results;
using static x42.Server.RuntimeStats;

namespace x42.Server
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
        private readonly NetworkFeatures networkFeatures;
        private readonly X42ClientFeature x42FullNode;
        private readonly DatabaseFeatures database;
        private readonly DatabaseSettings databaseSettings;
        private readonly SetupServer setupServer;
        private readonly ServerFunctions serverFunctions;
        private readonly ProfileFunctions profileFunctions;

        public RuntimeStats Stats { get; set; } = new RuntimeStats();

        /// <summary>Creates new instance of the <see cref="XServer" />.</summary>
        public XServer(NetworkFeatures networkFeatures,
            ServerSettings nodeSettings,
            X42ClientFeature x42FullNode,
            DatabaseFeatures database,
            DatabaseSettings databaseSettings)
        {
            this.networkFeatures = networkFeatures;
            this.nodeSettings = nodeSettings;
            this.x42FullNode = x42FullNode;
            this.database = database;
            this.databaseSettings = databaseSettings;

            setupServer = new SetupServer(databaseSettings.ConnectionString, database);
            serverFunctions = new ServerFunctions(databaseSettings.ConnectionString);
            profileFunctions = new ProfileFunctions(databaseSettings.ConnectionString);

            State = XServerState.Created;
            Stats = new RuntimeStats();
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
                                       Assembly.GetEntryAssembly().GetName().Version.ToString();

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

        public uint BestBlockHeight { get => x42FullNode.BlockTIP; }

        public uint? AddressIndexerHeight { get => x42FullNode.AddressIndexterTip; }

        public async Task<RegisterResult> Register(ServerNodeData serverNode)
        {
            return await networkFeatures.Register(serverNode);
        }

        public void Start(StartRequest startRequest)
        {
            Stats.Reset(true);
            var connectionInfo = new CachedServerInfo()
            {
                AccountName = startRequest.AccountName,
                SignAddress = startRequest.SignAddress,
                Password = startRequest.Password,
                WalletName = startRequest.WalletName
            };
            networkFeatures.Connect(connectionInfo);
            // TODO: Start serving apps.
        }

        public void Stop()
        {
            Stats.Reset(false);
            Stats.UpdateTierLevel(Tier.TierLevel.Seed);
            // TODO: Stop serving apps.
        }

        /// <inheritdoc />
        public void StartFeature()
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
            StartPeriodicChecks();

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

        /// <summary>
        ///     Starts a loop to periodically check tier level.
        /// </summary>
        private void StartPeriodicChecks()
        {
            periodicLogLoop = AsyncLoopFactory.Run("PeriodicChecks", cancellation =>
            {
                var serverSetupResult = GetServerSetupStatus();

                Stats.StartupState = networkFeatures.GetStartupStatus();
                Stats.BlockHeight = networkFeatures.BestBlockHeight;
                Stats.AddressIndexerHeight = networkFeatures.AddressIndexerHeight ?? 0;

                // Only update tier level when running.
                if (Stats.State == (int)RuntimeState.Started && networkFeatures.IsServerReady())
                {
                    var blocksBehind = Stats.BlockHeight - Stats.AddressIndexerHeight;
                    if (blocksBehind > 6) // TODO: Need to use the BlockGracePeriod instead of setting this manually to 6
                    {
                        Stats.UpdateTierLevel(Tier.TierLevel.Seed);
                    }
                    else
                    {
                        Stats.UpdateTierLevel(serverSetupResult.TierLevel);
                    }
                }

                return Task.CompletedTask;
            },
                serverLifetime.ApplicationStopping,
                TimeSpans.FiveSeconds,
                TimeSpans.Second);
        }

        /// <inheritdoc />
        public async Task<string> SetupServer(SetupRequest setupRequest = null)
        {
            Guard.NotNull(setupRequest, nameof(setupRequest));

            string result = string.Empty;

            var profile = profileFunctions.GetProfileByKeyAddress(setupRequest.KeyAddress);
            if (profile != null)
            {
                if (string.IsNullOrEmpty(setupRequest.SignAddress))
                {
                    string signAddress = setupServer.GetSignAddress();
                    if (string.IsNullOrEmpty(signAddress))
                    {
                        setupRequest.SignAddress = await networkFeatures.GetServerAddress("x42ServerMain");
                        AddServerAddress(setupRequest, profile.Name);
                        result = setupRequest.SignAddress;
                    }
                    else
                    {
                        result = signAddress;
                        setupServer.UpdateServerProfileName(profile.Name);
                    }
                }
                else
                {
                    AddServerAddress(setupRequest, profile.Name);
                }
            }
            return result;
        }

        private bool AddServerAddress(SetupRequest setupRequest, string profileName)
        {
            bool result = false;
            try
            {
                result = setupServer.AddServerToSetup(setupRequest, profileName);
            }
            catch (Exception ex)
            {
                logger.LogError("ERROR Adding server address", ex);
            }

            return result;
        }

        /// <inheritdoc />
        public SetupStatusResult GetServerSetupStatus()
        {
            return setupServer.GetServerSetupStatus();
        }

        /// <inheritdoc />
        public TopResult GetTopXServers(int top)
        {
            int maxResults = 100;
            if (top > maxResults)
            {
                top = maxResults;
            }
            return serverFunctions.GetTopXServers(top);
        }

        /// <inheritdoc />
        public int GetActiveServerCount()
        {
            return serverFunctions.GetActiveServerCount();
        }

        /// <inheritdoc />
        public List<ServerRegisterResult> GetActiveXServers(int fromId)
        {
            return serverFunctions.GetActiveXServers(fromId);
        }

        /// <inheritdoc />
        public ServerRegisterResult SearchForXServer(string profileName = "", string signAddress = "")
        {
            return serverFunctions.SearchForXServer(profileName, signAddress);
        }

        public string GetMyFeeAddress()
        {
            return networkFeatures.GetMyFeeAddress();
        }

        public string GetMyPublicKey()
        {
            return networkFeatures.GetMyPublicKey();
        }

        public string GetServerProfileName()
        {
            string result = string.Empty;
            if (networkFeatures.IsServerReady())
            {
                result = networkFeatures.GetServerProfile();
            }
            return result;
        }
    }
}