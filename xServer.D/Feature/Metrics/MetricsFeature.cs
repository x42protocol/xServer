using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using x42.Configuration.Logging;
using x42.Feature.Setup;
using x42.ServerNode;
using x42.Server;
using x42.Feature.Metrics.Models;
using x42.Utilities;

namespace x42.Feature.Metrics
{
    /// <inheritdoc />
    /// <summary>
    ///     Provides an ability to serve Metrics.
    /// </summary>
    public class MetricsFeature : ServerFeature
    {

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime _serverLifetime;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory _asyncLoopFactory;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Instance logger.</summary>

        private readonly MetricsService _metricsService;
        private MetricsMonitor _metricsMonitor;


        public MetricsFeature(
            ServerNodeBase network,
            ILoggerFactory loggerFactory,
            MetricsService metricsService,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory)
        {
            logger = loggerFactory.CreateLogger(GetType().FullName);
            _metricsService = metricsService;
            _serverLifetime = serverLifetime;
            _asyncLoopFactory = asyncLoopFactory;

        }

        public HostStatsModel getHardwareMetricsAsync()
        {
            return _metricsService.GetHardwareMetric();
        }

      
        /// <inheritdoc />
        public override void Dispose()
        {
            
        }

        public override Task InitializeAsync()
        {
            _metricsMonitor = new MetricsMonitor(logger, _serverLifetime, _asyncLoopFactory, this);

            _metricsMonitor.Start();

            logger.LogInformation("Metrics Feature Initialized");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="MetricsFeature" />.
    /// </summary>
    public static class MetricsBuilderExtension
    {
        /// <summary>
        ///     Adds Metrics components to the server.
        /// </summary>
        /// <param name="serverBuilder">The object used to build the current node.</param>
        /// <returns>The server builder, enriched with the new component.</returns>
        public static IServerBuilder UseMetrics(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<MetricsFeature>("metrics");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<MetricsFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<MetricsFeature>();
                        services.AddSingleton<MetricsService>();
                    });
            });

            return serverBuilder;
        }
    }
}