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
        private readonly int _checkSleepSeconds = 10;

        private MetricsFeature _metricsFeature;

        public List<int> CPUUtilization { get; }

        public MetricsMonitor(
            ILogger mainLogger,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory,
            MetricsFeature metricsFeature
            )
        {
            logger = mainLogger;
            this._serverLifetime = serverLifetime;
            this._asyncLoopFactory = asyncLoopFactory;
            _metricsFeature = metricsFeature;
        }

        public void Start()
        {
            this._metricsMonitorCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { _serverLifetime.ApplicationStopping });

            Startup().ConfigureAwait(false);

            _asyncLoopFactory.Run("MetricsManager.CPUMetricsWorker", async token =>
            {
                try
                {

                    await UpdateCPUMetricsCounters().ConfigureAwait(false);
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
          

        public async Task UpdateCPUMetricsCounters()
        {
            try
            {
                await CountCPUUtalizationAsync();
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Error in UpdateNetworkHealth", ex);
            }
        }

        public async Task Startup()
        {
    
        }

        /// <summary>
        ///     Check for active servers, and remove any inactive servers.
        /// </summary>
        private async Task CountCPUUtalizationAsync()
        {
            //Populate a round robin array (60 Slots of every 10 seconds)
            //This array must be accessible to allow the MetricsService to Read from it.
            throw new NotImplementedException();
        }


        public void Dispose()
        {
        }

    }
}
