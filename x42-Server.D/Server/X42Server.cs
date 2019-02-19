using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using X42.Configuration;
using X42.Utilities;
using X42.MasterNode;

namespace X42.Server
{
    /// <summary>
    /// Server providing all supported features of the blockchain and its network.
    /// </summary>
    public class X42Server : IX42Server
    {
        /// <summary>Instance logger.</summary>
        private ILogger logger;

        /// <summary>Factory for creating loggers.</summary>
        private ILoggerFactory loggerFactory;
        
        /// <summary>Server command line and configuration file settings.</summary>
        public ServerSettings Settings { get; private set; }
        
        /// <inheritdoc />
        public X42ServerState State { get; private set; }

        /// <inheritdoc />
        public DateTime StartTime { get; set; }
      
        /// <summary>Factory for creating and execution of asynchronous loops.</summary>
        public IAsyncLoopFactory AsyncLoopFactory { get; set; }

        /// <summary>Specification of the network the server runs on - regtest/testnet/mainnet.</summary>
        public MasterNodeBase MasterNode { get; internal set; }

        /// <summary>Contains path locations to folders and files on disk.</summary>
        public DataFolder DataFolder { get; set; }

        /// <summary>Provider of date time functionality.</summary>
        public IDateTimeProvider DateTimeProvider { get; set; }

        /// <summary>Application life cycle control - triggers when application shuts down.</summary>
        private X42ServerLifetime serverLifetime;

        /// <see cref="IServerStats"/>
        private IServerStats ServerStats { get; set; }

        private IAsyncLoop periodicLogLoop;
        
        /// <inheritdoc />
        public IX42ServerLifetime X42ServerLifetime
        {
            get { return this.serverLifetime; }
            private set { this.serverLifetime = (X42ServerLifetime)value; }
        }


        /// <inheritdoc />
        public IServerServiceProvider Services { get; set; }

        public T ServerService<T>(bool failWithDefault = false)
        {
            if (this.Services != null && this.Services.ServiceProvider != null)
            {
                var service = this.Services.ServiceProvider.GetService<T>();
                if (service != null)
                    return service;
            }

            if (failWithDefault)
                return default(T);

            throw new InvalidOperationException($"The {typeof(T).ToString()} service is not supported");
        }

        public T ServerFeature<T>(bool failWithError = false)
        {
            if (this.Services != null)
            {
                T feature = this.Services.Features.OfType<T>().FirstOrDefault();
                if (feature != null)
                    return feature;
            }

            if (!failWithError)
                return default(T);

            throw new InvalidOperationException($"The {typeof(T).ToString()} feature is not supported");
        }


        /// <inheritdoc />
        public Version Version
        {
            get
            {
                string versionString = typeof(X42Server).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ??
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion;

                if (!string.IsNullOrEmpty(versionString))
                {
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
                }

                return new Version(0, 0);
            }
        }

        /// <summary>Creates new instance of the <see cref="X42Server"/>.</summary>
        public X42Server()
        {
            this.State = X42ServerState.Created;
        }

        /// <inheritdoc />
        public X42Server Initialize(ServerServiceProvider serviceProvider)
        {
            this.State = X42ServerState.Initializing;

            Guard.NotNull(serviceProvider, nameof(serviceProvider));

            this.Services = serviceProvider;

            this.logger = this.Services.ServiceProvider.GetService<ILoggerFactory>().CreateLogger(this.GetType().FullName);

            this.DataFolder = this.Services.ServiceProvider.GetService<DataFolder>();
            this.DateTimeProvider = this.Services.ServiceProvider.GetService<IDateTimeProvider>();
            this.MasterNode = this.Services.ServiceProvider.GetService<MasterNodeBase>();
            this.Settings = this.Services.ServiceProvider.GetService<ServerSettings>();
            this.ServerStats = this.Services.ServiceProvider.GetService<IServerStats>();
            
            this.loggerFactory = this.Services.ServiceProvider.GetService<ServerSettings>().LoggerFactory;

            this.AsyncLoopFactory = this.Services.ServiceProvider.GetService<IAsyncLoopFactory>();

            this.logger.LogInformation(x42.Properties.Resources.AsciiLogo);
            this.logger.LogInformation("x42 Master Node initialized on {0}.", this.MasterNode.Name);

            this.State = X42ServerState.Initialized;
            this.StartTime = this.DateTimeProvider.GetUtcNow();
            return this;
        }

        /// <inheritdoc />
        public void Start()
        {
            this.State = X42ServerState.Starting;

            if (this.State == X42ServerState.Disposing || this.State == X42ServerState.Disposed)
                throw new ObjectDisposedException(nameof(X42Server));

            this.serverLifetime = this.Services.ServiceProvider.GetRequiredService<IX42ServerLifetime>() as X42ServerLifetime;

            if (this.serverLifetime == null)
                throw new InvalidOperationException($"{nameof(IX42ServerLifetime)} must be set.");
            
            this.logger.LogInformation("Starting server.");
            
            // Fire IServerLifetime.Started.
            this.serverLifetime.NotifyStarted();

            this.StartPeriodicLog();

            this.State = X42ServerState.Started;
        }

        /// <summary>
        /// Starts a loop to periodically log statistics about server's status very couple of seconds.
        /// <para>
        /// These logs are also displayed on the console.
        /// </para>
        /// </summary>
        private void StartPeriodicLog()
        {
            this.periodicLogLoop = this.AsyncLoopFactory.Run("PeriodicLog", (cancellation) =>
            {
                string stats = this.ServerStats.GetStats();

                this.logger.LogInformation(stats);
                this.LastLogOutput = stats;

                return Task.CompletedTask;
            },
            this.serverLifetime.ApplicationStopping,
            repeatEvery: TimeSpans.FiveSeconds,
            startAfter: TimeSpans.FiveSeconds);
        }

        public string LastLogOutput { get; private set; }
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (this.State == X42ServerState.Disposing || this.State == X42ServerState.Disposed)
                return;

            this.State = X42ServerState.Disposing;

            this.logger.LogInformation("Closing server pending.");

            // Fire IServerLifetime.Stopping.
            this.serverLifetime.StopApplication();
            
            this.logger.LogInformation("Disposing periodic logging loops.");
            this.periodicLogLoop?.Dispose();
            
            this.logger.LogInformation("Disposing settings.");
            this.Settings.Dispose();

            // Fire IServerLifetime.Stopped.
            this.logger.LogInformation("Notify application has stopped.");
            this.serverLifetime.NotifyStopped();

            this.State = X42ServerState.Disposed;
        }
    }
}