using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using x42.Feature.Database.Context;
using X42.Feature.Database;
using X42.Utilities;

namespace x42.Feature.Network
{
    public sealed partial class NetworkMonitor : IDisposable
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        ///     A cancellation token source that can cancel the node monitoring processes and is linked to the <see cref="IX42ServerLifetime.ApplicationStopping"/>.
        /// </summary>
        private CancellationTokenSource networkCancellationTokenSource;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IX42ServerLifetime serverLifetime;

        /// <summary>Loop in which the node attempts to maintain a connection with the x42 network.</summary>
        private IAsyncLoop networkMonitorLoop;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        /// <summary>Time in milliseconds between attempts run the network health monitor</summary>
        private readonly int monitorSleep = 10000;
        
        private readonly DatabaseSettings databaseSettings;

        public NetworkMonitor(
            ILogger mainLogger, 
            IX42ServerLifetime serverLifetime, 
            IAsyncLoopFactory asyncLoopFactory,
            DatabaseSettings databaseSettings
            )
        {
            logger = mainLogger;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
            this.databaseSettings = databaseSettings;
        }

        public void NetworkWorker()
        {
            this.networkCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { serverLifetime.ApplicationStopping });

            this.networkMonitorLoop = asyncLoopFactory.Run("NetworkManager.NetworkWorker", async token =>
            {
                try
                {
                    await UpdateNetworkHealth(networkCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromMilliseconds(this.monitorSleep),
            startAfter: TimeSpans.Second);
        }

        public async Task UpdateNetworkHealth(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CheckActiveServers().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogDebug($"Error in UpdateNetworkHealth", ex);
                }
            }
        }

        /// <summary>
        ///     Once a new block is staked, this method is used to verify that it
        /// </summary>
        /// <param name="block">The new block.</param>
        /// <param name="chainTip">Block that was considered as a chain tip when the block staking started.</param>
        private Task CheckActiveServers()
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

    }
}
