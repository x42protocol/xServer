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

namespace x42.Feature.Network
{
    public sealed partial class NetworkMonitor : IDisposable
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        ///     A cancellation token source that can cancel the node monitoring processes and is linked to the <see cref="IxServerLifetime.ApplicationStopping"/>.
        /// </summary>
        private CancellationTokenSource networkCancellationTokenSource;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime serverLifetime;

        /// <summary>Loop in which the node attempts to maintain a connection with the x42 network.</summary>
        private IAsyncLoop networkMonitorLoop;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        /// <summary>Time in milliseconds between attempts run the network health monitor</summary>
        private readonly int monitorSleep = 10000;

        /// <summary>Time in seconds between attempts run the relay monitor</summary>
        private readonly int relaySleepSeconds = 10;

        private readonly DatabaseSettings databaseSettings;

        private readonly NetworkFeatures networkFeatures;

        public NetworkMonitor(
            ILogger mainLogger,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory,
            DatabaseSettings databaseSettings,
            NetworkFeatures networkFeatures
            )
        {
            logger = mainLogger;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
            this.databaseSettings = databaseSettings;
            this.networkFeatures = networkFeatures;
        }

        public void Start()
        {
            this.networkCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { serverLifetime.ApplicationStopping });

            this.networkMonitorLoop = asyncLoopFactory.Run("NetworkManager.NetworkWorker", async token =>
            {
                try
                {
                    await UpdateNetworkHealth().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_NETHEALTH]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromMilliseconds(this.monitorSleep),
            startAfter: TimeSpans.Second);

            this.networkMonitorLoop = asyncLoopFactory.Run("NetworkManager.RelayWorker", async token =>
            {
                try
                {
                    await RelayService().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_RELAY]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromSeconds(this.relaySleepSeconds),
            startAfter: TimeSpans.Second);
        }

        public async Task UpdateNetworkHealth()
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

                List<Task<ServerNodeData>> nodeTasks = new List<Task<ServerNodeData>>();

                foreach (ServerNodeData serverNode in serverNodes)
                {
                    nodeTasks.Add(ServerCheck(serverNode));
                }

                var results = await Task.WhenAll(nodeTasks);
            }
        }

        public async Task RelayService()
        {
            try
            {
                await CheckRelayAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Error in UpdateNetworkHealth", ex);
            }
        }

        /// <summary>
        ///     Check for new xServers to relay to active xServers.
        /// </summary>
        private async Task CheckRelayAsync()
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<ServerNodeData> newServerNodes = dbContext.ServerNodes.Where(s => !s.Relayed);
                if (newServerNodes.Count() > 0)
                {
                    List<ServerNodeData> serverNodes = dbContext.ServerNodes.Where(s => s.Active).ToList();
                    var xServerStats = await networkFeatures.GetXServerStats();
                    foreach (var connectedXServer in xServerStats.Nodes)
                    {
                        var xServer = serverNodes.Where(x => x.NetworkAddress == connectedXServer.Address);
                        if (xServer.Count() == 0)
                        {
                            serverNodes.Add(new ServerNodeData()
                            {
                                Name = connectedXServer.Name,
                                NetworkAddress = connectedXServer.Address,
                                NetworkPort = connectedXServer.Port,
                                NetworkProtocol = 0 // TODO: Need to implement protocol.
                            });
                        }
                    }
                    foreach (ServerNodeData newServer in newServerNodes)
                    {
                        RelayXServer(newServer, serverNodes);
                        newServer.Relayed = true;
                    }
                    dbContext.SaveChanges();
                }
            }
        }

        public void RelayXServer(ServerNodeData newServer, List<ServerNodeData> activeXServers)
        {
            RegisterRequest registerRequest = new RegisterRequest()
            {
                Name = newServer.Name,
                Address = newServer.PublicAddress,
                NetworkAddress = newServer.NetworkAddress,
                NetworkPort = newServer.NetworkPort,
                NetworkProtocol = newServer.NetworkProtocol,
                Signature = newServer.Signature,
                Tier = newServer.Tier
            };

            foreach (var activeServer in activeXServers)
            {
                string xServerURL = networkFeatures.GetServerUrl(activeServer.NetworkProtocol, activeServer.NetworkAddress, activeServer.NetworkPort);
                var client = new RestClient(xServerURL);
                var registerRestRequest = new RestRequest("/register", Method.POST);
                var request = JsonConvert.SerializeObject(registerRequest);
                registerRestRequest.AddParameter("application/json; charset=utf-8", request, ParameterType.RequestBody);
                registerRestRequest.RequestFormat = DataFormat.Json;

                client.Execute(registerRestRequest);
            }
        }

        private async Task<ServerNodeData> ServerCheck(ServerNodeData serverNode)
        {
            bool serverIsValid = await networkFeatures.IsServerKeyValid(serverNode);

            if (serverIsValid)
            {
                if (serverNode.LastSeen > DateTime.UtcNow.AddHours(1))
                {
                    serverNode.Active = false;
                }
                else
                {
                    // TODO: Do the checks.

                    serverNode.Active = true;
                    serverNode.LastSeen = DateTime.UtcNow;
                }
            }

            return serverNode;
        }



        public void Dispose()
        {
        }

    }
}
