using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using X42.Feature.X42Client.Enums;
using X42.Feature.X42Client.RestClient;
using X42.Feature.X42Client.RestClient.Responses;
using X42.Feature.X42Client.Utils.Extensions;
using X42.Utilities;

namespace x42.Feature.Network
{
    public sealed partial class NetworkManager : IDisposable
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// A cancellation token source that can cancel the node monitoring processes and is linked to the <see cref="IX42ServerLifetime.ApplicationStopping"/>.
        /// </summary>
        private CancellationTokenSource nodeCancellationTokenSource;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IX42ServerLifetime serverLifetime;

        /// <summary>Loop in which the node attempts to maintain a connection with the x42 node.</summary>
        private IAsyncLoop nodeMonitorLoop;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        /// <summary>Time in milliseconds between attempts to connect to x42 node.</summary>
        private readonly int monitorSleep = 1000;

        public NetworkManager(string name, IPAddress address, uint port, ILogger mainLogger, IX42ServerLifetime serverLifetime, IAsyncLoopFactory asyncLoopFactory)
        {
            logger = mainLogger;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
        }

        public void NetworkWorker()
        {
            this.nodeCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { this.serverLifetime.ApplicationStopping });

            this.nodeMonitorLoop = this.asyncLoopFactory.Run("NetworkManager.NetworkWorker", async token =>
            {
                try
                {
                    UpdateNetworkHealth(nodeCancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION]");
                    throw;
                }
            },
            this.nodeCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromMilliseconds(this.monitorSleep),
            startAfter: TimeSpans.Second);
        }

        public void UpdateNetworkHealth(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                   
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"Error in UpdateNetworkHealth", ex);
                }
            }
        }

        public void Dispose()
        {
        }

    }
}
