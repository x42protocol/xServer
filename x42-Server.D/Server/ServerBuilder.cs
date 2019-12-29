using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using X42.Configuration;
using X42.Feature.Setup;
using X42.ServerNode;
using X42.Utilities;

namespace X42.Server
{
    /// <summary>
    ///     Exception thrown by ServerBuilder.Build.
    /// </summary>
    /// <seealso cref="ServerBuilder.Build" />
    public class ServerBuilderException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ServerBuilderException(string message) : base(message)
        {
        }
    }

    /// <summary>
    ///     x42 server builder allows constructing a x42 server using specific components.
    /// </summary>
    public class ServerBuilder : IServerBuilder
    {
        /// <summary>List of delegates that configure the service providers.</summary>
        private readonly List<Action<IServiceProvider>> configureDelegates;

        /// <summary>List of delegates that add services to the builder.</summary>
        private readonly List<Action<IServiceCollection>> configureServicesDelegates;

        /// <summary>List of delegates that add features to the collection.</summary>
        private readonly List<Action<IFeatureCollection>> featuresRegistrationDelegates;

        /// <summary>true if the Build method has been called already (whether it succeeded or not), false otherwise.</summary>
        private bool serverBuilt;

        /// <summary>
        ///     Initializes a default instance of the object and registers required services.
        /// </summary>
        public ServerBuilder() :
            this(new List<Action<IServiceCollection>>(),
                new List<Action<IServiceProvider>>(),
                new List<Action<IFeatureCollection>>(),
                new FeatureCollection())
        {
        }

        /// <summary>
        ///     Initializes an instance of the object using specific ServerSettings instance and registers required services.
        /// </summary>
        /// <param name="serverSettings">User defined server settings.</param>
        public ServerBuilder(ServerSettings serverSettings)
            : this(serverSettings, new List<Action<IServiceCollection>>(),
                new List<Action<IServiceProvider>>(),
                new List<Action<IFeatureCollection>>(),
                new FeatureCollection())
        {
        }

        /// <summary>
        ///     Initializes an instance of the object using specific ServerSettings instance and configuration delegates and
        ///     registers required services.
        /// </summary>
        /// <param name="serverSettings">User defined server settings.</param>
        /// <param name="configureServicesDelegates">List of delegates that add services to the builder.</param>
        /// <param name="configureDelegates">List of delegates that configure the service providers.</param>
        /// <param name="featuresRegistrationDelegates">List of delegates that add features to the collection.</param>
        /// <param name="features">Collection of features to be available to and/or used by the server.</param>
        internal ServerBuilder(ServerSettings serverSettings,
            List<Action<IServiceCollection>> configureServicesDelegates,
            List<Action<IServiceProvider>> configureDelegates,
            List<Action<IFeatureCollection>> featuresRegistrationDelegates, IFeatureCollection features)
            : this(configureServicesDelegates, configureDelegates, featuresRegistrationDelegates, features)
        {
            Guard.NotNull(serverSettings, nameof(serverSettings));

            ServerSettings = serverSettings;
            ServerNode = ServerSettings.ServerNode;

            ConfigureServices(service =>
            {
                service.AddSingleton(ServerSettings);
                service.AddSingleton(ServerNode);
            });

            this.UseBaseFeature();
        }

        /// <summary>
        ///     Initializes an instance of the object using specific configuration delegates.
        /// </summary>
        /// <param name="configureServicesDelegates">List of delegates that add services to the builder.</param>
        /// <param name="configureDelegates">List of delegates that configure the service providers.</param>
        /// <param name="featuresRegistrationDelegates">List of delegates that add features to the collection.</param>
        /// <param name="features">Collection of features to be available to and/or used by the server.</param>
        internal ServerBuilder(List<Action<IServiceCollection>> configureServicesDelegates,
            List<Action<IServiceProvider>> configureDelegates,
            List<Action<IFeatureCollection>> featuresRegistrationDelegates, IFeatureCollection features)
        {
            Guard.NotNull(configureServicesDelegates, nameof(configureServicesDelegates));
            Guard.NotNull(configureDelegates, nameof(configureDelegates));
            Guard.NotNull(featuresRegistrationDelegates, nameof(featuresRegistrationDelegates));
            Guard.NotNull(features, nameof(features));

            this.configureServicesDelegates = configureServicesDelegates;
            this.configureDelegates = configureDelegates;
            this.featuresRegistrationDelegates = featuresRegistrationDelegates;
            Features = features;
        }

        /// <summary>Collection of features available to and/or used by the server.</summary>
        public IFeatureCollection Features { get; }

        /// <inheritdoc />
        public ServerSettings ServerSettings { get; set; }

        /// <inheritdoc />
        public ServerNodeBase ServerNode { get; set; }

        /// <summary>Collection of DI services.</summary>
        public IServiceCollection Services { get; private set; }

        /// <inheritdoc />
        public IServerBuilder ConfigureFeature(Action<IFeatureCollection> configureFeatures)
        {
            Guard.NotNull(configureFeatures, nameof(configureFeatures));

            featuresRegistrationDelegates.Add(configureFeatures);
            return this;
        }

        /// <inheritdoc />
        public IServerBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, nameof(configureServices));

            configureServicesDelegates.Add(configureServices);
            return this;
        }

        /// <inheritdoc />
        public IServerBuilder ConfigureServiceProvider(Action<IServiceProvider> configure)
        {
            Guard.NotNull(configure, nameof(configure));

            configureDelegates.Add(configure);
            return this;
        }

        /// <inheritdoc />
        public IX42Server Build()
        {
            if (serverBuilt)
                throw new InvalidOperationException("x42 server already built");
            serverBuilt = true;

            Services = BuildServices();

            // Print command - line help
            if (ServerSettings?.PrintHelpAndExit ?? false)
            {
                foreach (IFeatureRegistration featureRegistration in Features.FeatureRegistrations)
                {
                    MethodInfo printHelp =
                        featureRegistration.FeatureType.GetMethod("PrintHelp",
                            BindingFlags.Public | BindingFlags.Static);

                    printHelp?.Invoke(null, new object[] {ServerSettings.ServerNode});
                }

                // Signal server not built
                return null;
            }

            // Create configuration file if required
            ServerSettings?.CreateDefaultConfigurationFile(Features.FeatureRegistrations);

            ServiceProvider serverServiceProvider = Services.BuildServiceProvider();
            ConfigureServices(serverServiceProvider);

            // Obtain the serverSettings from the service (it's set used ServerBuilder.UseServerSettings)
            ServerSettings serverSettings = serverServiceProvider.GetService<ServerSettings>();
            if (serverSettings == null)
                throw new ServerBuilderException("ServerSettings not specified");

            ServerNodeBase serverNode = serverServiceProvider.GetService<ServerNodeBase>();
            if (serverNode == null)
                throw new ServerBuilderException("ServerNode not specified");

            X42Server server = serverServiceProvider.GetService<X42Server>();
            if (server == null)
                throw new InvalidOperationException("X42Server not registered with provider");

            server.Initialize(
                new ServerServiceProvider(
                    serverServiceProvider,
                    Features.FeatureRegistrations.Select(s => s.FeatureType).ToList()
                )
            );

            return server;
        }

        /// <summary>
        ///     Constructs and configures services ands features to be used by the server.
        /// </summary>
        /// <returns>Collection of registered services.</returns>
        private IServiceCollection BuildServices()
        {
            Services = new ServiceCollection();

            // register services before features
            // as some of the features may depend on independent services
            foreach (Action<IServiceCollection> configureServices in configureServicesDelegates)
                configureServices(Services);

            // configure features
            foreach (Action<IFeatureCollection> configureFeature in featuresRegistrationDelegates)
                configureFeature(Features);

            // configure features startup
            foreach (IFeatureRegistration featureRegistration in Features.FeatureRegistrations)
            {
                try
                {
                    featureRegistration.EnsureDependencies(Features.FeatureRegistrations);
                }
                catch (MissingDependencyException e)
                {
                    ServerSettings.Logger.LogCritical(
                        "Feature {0} cannot be configured because it depends on other features that were not registered",
                        featureRegistration.FeatureType.Name);

                    throw e;
                }

                featureRegistration.BuildFeature(Services);
            }

            return Services;
        }

        /// <summary>
        ///     Configure registered services.
        /// </summary>
        /// <param name="serviceProvider"></param>
        private void ConfigureServices(IServiceProvider serviceProvider)
        {
            foreach (Action<IServiceProvider> configure in configureDelegates)
                configure(serviceProvider);
        }
    }
}