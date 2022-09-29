using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using x42.Feature.Database.Context;
using x42.Feature.Database;
using x42.Utilities;
using x42.Feature.Database.Tables;
using System.Collections.Generic;
using x42.Controllers.Requests;
using RestSharp;
using Newtonsoft.Json;
using System.Net;
using x42.Controllers.Results;
using x42.ServerNode;
using static x42.ServerNode.Tier;
using x42.Feature.Metrics.Models;

namespace x42.Feature.Metrics
{
    public sealed partial class MetricsMonitor : IDisposable
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        ///     A cancellation token source that can cancel the node monitoring processes and is linked to the <see cref="IxServerLifetime.ApplicationStopping"/>.
        /// </summary>
        private CancellationTokenSource _metricsMonitorCancellationTokenSource;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime _serverLifetime;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory _asyncLoopFactory;

        /// <summary>Time in seconds between attempts run the network health monitor</summary>
        private readonly int _checkSleepSeconds = 5;

        private readonly MetricsService _metricsService;
        public double[] cpuMetrics = new double[12]; // one minute average, over 5 second checks
        public double[] memoryMetrics = new double[12]; // one minute average, over 5 second checks
        private int cpuMetricsPos = 0;
        private int memoryMetricsPos = 0;
        private bool _cpuMetricsInitialized = false;
        private bool _memoryMetricsInitialized = false;

        public MetricsMonitor(
            ILogger mainLogger,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory,
            MetricsService metricsService
            )
        {
            logger = mainLogger;
            this._serverLifetime = serverLifetime;
            this._asyncLoopFactory = asyncLoopFactory;
            _metricsService = metricsService;
        }


        public void Start()
        {
            this._metricsMonitorCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { _serverLifetime.ApplicationStopping });

            Startup().ConfigureAwait(false);

            _asyncLoopFactory.Run("MetricsManager.CPUMetricsWorker", async token =>
            {
                try
                {

                    await UpdateHostHardwareMetricsCounters().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_NETHEALTH]");
                    throw;
                }
            },
            _metricsMonitorCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromSeconds(this._checkSleepSeconds),
            startAfter: TimeSpans.Second);
        }
          
        public double GetCpuMetrics()
        {
            if (_cpuMetricsInitialized)
            {
                return cpuMetrics.Average();
            }
            else return new float() ;
        }

        public double GetMemoryMetrics()
        {
            if (_memoryMetricsInitialized)
            {
                return memoryMetrics.Average();
            }
            else return new double();
        }
        public async Task UpdateHostHardwareMetricsCounters()
        {
            try
            {
                var counters = CountHostHardwareUtalizationAsync();

                if (cpuMetricsPos == cpuMetrics.Length - 1 && !_cpuMetricsInitialized)
                { 
                    _cpuMetricsInitialized = true; 
                }

                cpuMetrics[cpuMetricsPos] = counters.ProcessorUtilizationPercent;
                cpuMetricsPos = cpuMetricsPos == cpuMetrics.Length - 1 ? 0 : cpuMetricsPos+1;

                if (memoryMetricsPos == memoryMetrics.Length - 1 && !_memoryMetricsInitialized)
                {
                    _memoryMetricsInitialized = true; 
                }

                memoryMetrics[memoryMetricsPos] = counters.AvailableMemoryMb;
                memoryMetricsPos = memoryMetricsPos == memoryMetrics.Length - 1 ? 0 : memoryMetricsPos+1;

            }
            catch (Exception ex)
            {
                logger.LogDebug($"Error in UpdateNetworkHealth", ex);
            }
        }

        public async Task Startup()
        {
            logger.LogInformation("Metrics Monitor Starting Up");
        }

        /// <summary>
        ///     Check for active servers, and remove any inactive servers.
        /// </summary>
        private HostStatsModel CountHostHardwareUtalizationAsync()
        {
            return _metricsService.GetHardwareMetric();
        }


        public void Dispose()
        {
        }

    }
}
