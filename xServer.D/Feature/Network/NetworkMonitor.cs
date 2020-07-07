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

        /// <summary>Time in seconds between attempts run the network health monitor</summary>
        private readonly int monitorSleep = 10;

        /// <summary>Time in seconds between attempts run the relay monitor</summary>
        private readonly int relaySleepSeconds = 10;

        /// <summary>Time in seconds between attempts to run the reconciliation process</summary>
        private readonly int serverRecoSleepSeconds = 86400;

        private readonly DatabaseSettings databaseSettings;

        private readonly NetworkFeatures networkFeatures;

        private readonly ServerNodeBase network;

        public NetworkMonitor(
            ILogger mainLogger,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory,
            DatabaseSettings databaseSettings,
            NetworkFeatures networkFeatures,
            ServerNodeBase network
            )
        {
            logger = mainLogger;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
            this.databaseSettings = databaseSettings;
            this.networkFeatures = networkFeatures;
            this.network = network;
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
            repeatEvery: TimeSpan.FromSeconds(this.monitorSleep),
            startAfter: TimeSpans.Second);

            this.networkMonitorLoop = asyncLoopFactory.Run("NetworkManager.RelayWorker", async token =>
            {
                try
                {
                    await RelayService(this.networkCancellationTokenSource.Token).ConfigureAwait(false);
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

            this.networkMonitorLoop = asyncLoopFactory.Run("NetworkManager.Reconciliation", async token =>
            {
                try
                {
                    await RecoServiceAsync(this.networkCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_RECO]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromSeconds(this.serverRecoSleepSeconds),
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
                IQueryable<ServerNodeData> allServerNodes = dbContext.ServerNodes;

                List<Task<ServerNodeData>> nodeTasks = new List<Task<ServerNodeData>>();

                foreach (ServerNodeData serverNode in allServerNodes)
                {
                    nodeTasks.Add(ServerCheck(serverNode));
                }
                await Task.WhenAll(nodeTasks);
                dbContext.SaveChanges();

                // Remove any servers that have been available past the grace period.
                var inactiveServers = allServerNodes.Where(n => n.Active == false);
                foreach (ServerNodeData serverNode in inactiveServers)
                {
                    var lastSeen = serverNode.LastSeen.AddMinutes(network.DowntimeGracePeriod);
                    if (DateTime.UtcNow > lastSeen)
                    {
                        dbContext.ServerNodes.Remove(serverNode);
                    }
                }
            }

            // Remove any servers that have been unavailable past the grace period.
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<ServerNodeData> inactiveServers = dbContext.ServerNodes.Where(n => !n.Active);
                foreach (ServerNodeData serverNode in inactiveServers)
                {
                    var lastSeen = serverNode.LastSeen.AddMinutes(network.DowntimeGracePeriod);
                    if (DateTime.UtcNow > lastSeen)
                    {
                        dbContext.ServerNodes.Remove(serverNode);
                    }
                }
                dbContext.SaveChanges();
            }
        }

        public async Task RelayService(CancellationToken cancellationToken)
        {
            try
            {
                await CheckRelayAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Error in Relay Service", ex);
            }
        }

        /// <summary>
        ///     Check for new xServers to relay to active xServers.
        /// </summary>
        private async Task CheckRelayAsync(CancellationToken cancellationToken)
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
                                NetworkAddress = connectedXServer.Address,
                                NetworkPort = connectedXServer.Port,
                                NetworkProtocol = connectedXServer.NetworkProtocol
                            });
                        }
                    }
                    foreach (ServerNodeData newServer in newServerNodes)
                    {
                        await RelayXServerAsync(newServer, serverNodes, cancellationToken);
                        newServer.Relayed = true;
                    }
                    dbContext.SaveChanges();
                }
            }
        }

        public async Task RelayXServerAsync(ServerNodeData newServer, List<ServerNodeData> activeXServers, CancellationToken cancellationToken)
        {
            RegisterRequest registerRequest = new RegisterRequest()
            {
                KeyAddress = newServer.KeyAddress,
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

                await client.ExecuteAsync(registerRestRequest, cancellationToken);
            }
        }

        private async Task<ServerNodeData> ServerCheck(ServerNodeData serverNode)
        {
            var serverCheck = await networkFeatures.Register(serverNode, serverCheckOnly: true);
            if (serverCheck.Success)
            {
                serverNode.Active = true;
                serverNode.LastSeen = DateTime.UtcNow;
            }
            else
            {
                serverNode.Active = false;
            }
            return serverNode;
        }

        public async Task RecoServiceAsync(CancellationToken cancellationToken)
        {
            await ReconciliationAsync(cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        ///     Reconcile with other active xServers to check for discrepancies.
        ///     This function will only add newly discovered xServers.
        /// </summary>
        private async Task ReconciliationAsync(CancellationToken cancellationToken)
        {
            var xServerUrls = await LocateAndAddNewServers(cancellationToken);

            if (xServerUrls.Count() > 0)
            {
                await AddSelfToNetwork(xServerUrls, cancellationToken);
            }
        }

        private async Task AddSelfToNetwork(List<string> xServerUrls, CancellationToken cancellationToken)
        {
            var selfNode = networkFeatures.GetSelfServerNode();
            if (selfNode.Active)
            {
                RegisterRequest registerRequest = new RegisterRequest()
                {
                    KeyAddress = selfNode.KeyAddress,
                    NetworkProtocol = selfNode.NetworkProtocol,
                    NetworkAddress = selfNode.NetworkAddress,
                    NetworkPort = selfNode.NetworkPort,
                    Signature = selfNode.Signature,
                    Tier = selfNode.Tier
                };
                foreach (string xServerURL in xServerUrls)
                {
                    var client = new RestClient(xServerURL);
                    var registerRestRequest = new RestRequest("/register", Method.POST);
                    var request = JsonConvert.SerializeObject(registerRequest);
                    registerRestRequest.AddParameter("application/json; charset=utf-8", request, ParameterType.RequestBody);
                    registerRestRequest.RequestFormat = DataFormat.Json;

                    await client.ExecuteAsync(registerRestRequest, cancellationToken);
                }
            }
        }

        private async Task<List<string>> LocateAndAddNewServers(CancellationToken cancellationToken)
        {
            List<string> result = new List<string>();

            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                List<ServerNodeData> serverNodes = dbContext.ServerNodes.Where(s => s.Active).ToList();
                string selfKeyAddress = networkFeatures.GetServerKeyAddress();
                int localActiveCount = serverNodes.Count();
                var xServerStats = await networkFeatures.GetXServerStats();
                if (xServerStats == null)
                {
                    return result;
                }
                foreach (var connectedXServer in xServerStats.Nodes)
                {
                    var xServer = serverNodes.Where(x => x.NetworkAddress == connectedXServer.Address);
                    if (xServer.Count() == 0)
                    {
                        serverNodes.Add(new ServerNodeData()
                        {
                            NetworkAddress = connectedXServer.Address,
                            NetworkPort = connectedXServer.Port,
                            NetworkProtocol = connectedXServer.NetworkProtocol
                        });
                    }
                }

                if (serverNodes.Count() > 0)
                {
                    List<ServerNodeData> newserverNodes = new List<ServerNodeData>();
                    foreach (ServerNodeData server in serverNodes)
                    {
                        bool foundSelf = false;
                        try
                        {
                            string xServerURL = networkFeatures.GetServerUrl(server.NetworkProtocol, server.NetworkAddress, server.NetworkPort);
                            var client = new RestClient(xServerURL);
                            var activeServerCountRequest = new RestRequest("/getactivecount/", Method.GET);
                            var topXServerResult = await client.ExecuteAsync<CountResult>(activeServerCountRequest, cancellationToken).ConfigureAwait(false);
                            if (topXServerResult.StatusCode == HttpStatusCode.OK)
                            {
                                var remoteCountResult = topXServerResult.Data;
                                if (remoteCountResult.Count > localActiveCount) // TODO: Need a better way to do this, both servers can have the same count with diffrent set of active servers, perhaps a hash of all of the active server signatures.
                                {
                                    var allActiveXServersRequest = new RestRequest("/getallactivexservers/", Method.GET);
                                    var allActiveXServersResult = await client.ExecuteAsync<List<RegisterRequest>>(allActiveXServersRequest, cancellationToken).ConfigureAwait(false);
                                    if (allActiveXServersResult.StatusCode == HttpStatusCode.OK)
                                    {
                                        var activeXServersList = allActiveXServersResult.Data;
                                        foreach (var serverResult in activeXServersList)
                                        {
                                            if (serverResult.KeyAddress == selfKeyAddress)
                                            {
                                                foundSelf = true;
                                            }
                                            var registeredServer = serverNodes.Where(s => s.Signature == serverResult.Signature).FirstOrDefault();
                                            if (registeredServer == null)
                                            {
                                                // Local Registration of new nodes we don't know about.
                                                await networkFeatures.Register(new ServerNodeData()
                                                {
                                                    KeyAddress = serverResult.KeyAddress,
                                                    NetworkAddress = serverResult.NetworkAddress,
                                                    NetworkPort = serverResult.NetworkPort,
                                                    NetworkProtocol = serverResult.NetworkProtocol,
                                                    Signature = serverResult.Signature,
                                                    Tier = serverResult.Tier
                                                });
                                            }
                                        }
                                        if (!foundSelf)
                                        {
                                            result.Add(xServerURL);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug($"Error in Reconciliation Service", ex);
                        }
                    }
                }
            }
            return result;
        }

        public void Dispose()
        {
        }

    }
}
