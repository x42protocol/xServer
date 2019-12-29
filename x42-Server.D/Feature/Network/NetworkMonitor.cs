using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using X42.Feature.Database.Context;
using X42.Feature.Database;
using X42.Feature.X42Client;
using X42.Utilities;
using X42.Feature.Database.Tables;

namespace X42.Feature.Network
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

        private X42Node x42Client;

        private readonly X42ClientSettings x42ClientSettings;

        public NetworkMonitor(
            ILogger mainLogger,
            IX42ServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory,
            DatabaseSettings databaseSettings,
            X42ClientSettings x42ClientSettings
            )
        {
            logger = mainLogger;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
            this.databaseSettings = databaseSettings;
            this.x42ClientSettings = x42ClientSettings;

            x42Client = new X42Node(x42ClientSettings.Name, x42ClientSettings.Address, x42ClientSettings.Port, logger, serverLifetime, asyncLoopFactory, false);
        }

        public void Start()
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
                    await CheckActiveServersAsync().ConfigureAwait(false);
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
        private async Task CheckActiveServersAsync()
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<ServerNodeData> serverNodes = dbContext.ServerNodes.Where(s => s.Active);
                foreach (ServerNodeData serverNode in serverNodes)
                {
                    if (await ValidateServer(serverNode))
                    {
                        // TODO: Test server.
                    }
                }
            }
        }

        private async Task<bool> ValidateServer(ServerNodeData servernode)
        {
            bool result = false;

            string serverKey = $"{servernode.Id}{servernode.Ip}{servernode.Port}{servernode.HAddress}";

            result = await x42Client.VerifyMessageAsync(servernode.CAddress, serverKey, servernode.Signature);

            return result;
        }

        public void Dispose()
        {
        }

    }
}
