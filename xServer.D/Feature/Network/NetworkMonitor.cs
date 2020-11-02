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

        /// <summary>The status of the x42 network startup.</summary>
        public StartupStatus NetworkStartupStatus;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        /// <summary>Time in seconds between attempts run the network health monitor</summary>
        private readonly int heckCheckSleepSeconds = 180;

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

            NetworkStartupStatus = StartupStatus.NotStarted;
        }

        public void Start()
        {
            this.networkCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { serverLifetime.ApplicationStopping });

            Startup().ConfigureAwait(false);

            asyncLoopFactory.Run("NetworkManager.NetworkWorker", async token =>
            {
                try
                {
                    if (networkFeatures.IsServerReady())
                    {
                        await UpdateNetworkHealth().ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_NETHEALTH]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromSeconds(this.heckCheckSleepSeconds),
            startAfter: TimeSpans.Second);

            asyncLoopFactory.Run("NetworkManager.NewxServer", async token =>
            {
                try
                {
                    await RelayNewxServerAsync(this.networkCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_XS_RELAY]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromSeconds(this.relaySleepSeconds),
            startAfter: TimeSpans.Second);

            asyncLoopFactory.Run("NetworkManager.xServerReconciliation", async token =>
            {
                try
                {
                    if (networkFeatures.IsServerReady())
                    {
                        await ReconcilexServersAsync(this.networkCancellationTokenSource.Token).ConfigureAwait(false);
                    }
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
            startAfter: TimeSpans.TenSeconds);

            asyncLoopFactory.Run("NetworkManager.NewPriceLock", async token =>
            {
                try
                {
                    await RelayNewPayLocks(this.networkCancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_PL_RELAY]");
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
                await CheckActiveServersAsync();
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Error in UpdateNetworkHealth", ex);
            }
        }

        public async Task Startup()
        {
            NetworkStartupStatus = StartupStatus.NodeAndDB;
            while (!networkFeatures.IsServerConnected())
            {
                Thread.Sleep(5000);
            }

            NetworkStartupStatus = StartupStatus.IBD;
            var blockchainInfo = await networkFeatures.GetBlockchainInfo();
            while (blockchainInfo.initialblockdownload)
            {
                Thread.Sleep(5000);
                blockchainInfo = await networkFeatures.GetBlockchainInfo();
            }

            NetworkStartupStatus = StartupStatus.AddressIndexer;
            blockchainInfo = await networkFeatures.GetBlockchainInfo();
            var addressIndexerTip = await networkFeatures.GetAddressIndexerTip();
            while (blockchainInfo.blocks != addressIndexerTip.tipHeight)
            {
                Thread.Sleep(5000);
                addressIndexerTip = await networkFeatures.GetAddressIndexerTip();
                blockchainInfo = await networkFeatures.GetBlockchainInfo();
            }

            NetworkStartupStatus = StartupStatus.XServer;
            int tier2Count = 0;
            int tier3Count = 0;
            while (tier2Count == 0 || tier3Count == 0)
            {
                var xServers = await networkFeatures.GetXServerStats();
                tier2Count = xServers.Nodes.Where(n => n.Tier == (int)TierLevel.Two).Count();
                tier3Count = xServers.Nodes.Where(n => n.Tier == (int)TierLevel.Two).Count();
                Thread.Sleep(5000);
            }

            NetworkStartupStatus = StartupStatus.Profile;
            await networkFeatures.SyncProfiles(this.networkCancellationTokenSource.Token);

            NetworkStartupStatus = StartupStatus.XServer;
            await ReconcilexServersAsync(this.networkCancellationTokenSource.Token);

            NetworkStartupStatus = StartupStatus.Started;
        }

        /// <summary>
        ///     Check for active servers, and remove any inactive servers.
        /// </summary>
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
                    else
                    {
                        var serverTier = await networkFeatures.GetServerTier(serverNode, network.BlockGracePeriod);
                        if (serverTier == null)
                        {
                            dbContext.ServerNodes.Remove(serverNode);
                        }
                    }
                }
                dbContext.SaveChanges();
            }
        }

        /// <summary>
        ///     Check for new xServers to relay to active xServers.
        /// </summary>
        private async Task RelayNewxServerAsync(CancellationToken cancellationToken)
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
                        var xServer = serverNodes.Where(x => x.NetworkAddress == connectedXServer.NetworkAddress);
                        if (xServer.Count() == 0)
                        {
                            serverNodes.Add(new ServerNodeData()
                            {
                                NetworkAddress = connectedXServer.NetworkAddress,
                                NetworkPort = connectedXServer.NetworkPort,
                                NetworkProtocol = connectedXServer.NetworkProtocol
                            });
                        }
                    }
                    foreach (ServerNodeData newServer in newServerNodes)
                    {
                        try
                        {
                            await RelayXServerAsync(newServer, serverNodes, cancellationToken);
                            newServer.Relayed = true;
                        }
                        catch (Exception) { }
                    }
                    dbContext.SaveChanges();
                }
            }
        }

        /// <summary>
        ///     Check for new paylocks and relay the information.
        /// </summary>
        private async Task RelayNewPayLocks(CancellationToken cancellationToken)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<PriceLockData> newPriceLocks = dbContext.PriceLocks.Where(p => !p.Relayed);
                if (newPriceLocks.Count() > 0)
                {
                    List<ServerNodeData> tier3Nodes = dbContext.ServerNodes.Where(s => s.Active && s.Tier == (int)TierLevel.Three).ToList();
                    foreach (var newPriceLock in newPriceLocks)
                    {
                        try
                        {
                            await networkFeatures.RelayPriceLock(newPriceLock, tier3Nodes, cancellationToken);
                            newPriceLock.Relayed = true;
                        }
                        catch (Exception) { }
                    }
                    dbContext.SaveChanges();
                }
            }
        }

        public async Task RelayXServerAsync(ServerNodeData newServer, List<ServerNodeData> activeXServers, CancellationToken cancellationToken)
        {
            ServerRegisterRequest registerRequest = new ServerRegisterRequest()
            {
                ProfileName = newServer.ProfileName,
                NetworkAddress = newServer.NetworkAddress,
                NetworkPort = newServer.NetworkPort,
                NetworkProtocol = newServer.NetworkProtocol,
                Signature = newServer.Signature,
                Tier = newServer.Tier,
                FeeAddress = newServer.FeeAddress,
                KeyAddress = newServer.KeyAddress,
                SignAddress = newServer.SignAddress
            };

            foreach (var activeServer in activeXServers)
            {
                string xServerURL = networkFeatures.GetServerUrl(activeServer.NetworkProtocol, activeServer.NetworkAddress, activeServer.NetworkPort);
                var client = new RestClient(xServerURL);
                var registerRestRequest = new RestRequest("/registerserver", Method.POST);
                var request = JsonConvert.SerializeObject(registerRequest);
                registerRestRequest.AddParameter("application/json; charset=utf-8", request, ParameterType.RequestBody);
                registerRestRequest.RequestFormat = DataFormat.Json;

                var result = await client.ExecuteAsync(registerRestRequest, cancellationToken);
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

        /// <summary>
        ///     Reconcile with other active xServers to check for discrepancies.
        ///     This function will only add newly discovered xServers.
        /// </summary>
        private async Task ReconcilexServersAsync(CancellationToken cancellationToken)
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
                ServerRegisterRequest registerRequest = new ServerRegisterRequest()
                {
                    ProfileName = selfNode.ProfileName,
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
                string selfProfile = networkFeatures.GetServerProfile();
                int localActiveCount = serverNodes.Count();
                var xServerStats = await networkFeatures.GetXServerStats();
                if (xServerStats == null)
                {
                    return result;
                }
                foreach (var connectedXServer in xServerStats.Nodes)
                {
                    var xServer = serverNodes.Where(x => x.NetworkAddress == connectedXServer.NetworkAddress);
                    if (xServer.Count() == 0)
                    {
                        serverNodes.Add(new ServerNodeData()
                        {
                            NetworkAddress = connectedXServer.NetworkAddress,
                            NetworkPort = connectedXServer.NetworkPort,
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
                        bool xServerSyncComplete = false;
                        long lastId = 0;
                        try
                        {
                            string xServerURL = networkFeatures.GetServerUrl(server.NetworkProtocol, server.NetworkAddress, server.NetworkPort);
                            while (!xServerSyncComplete)
                            {
                                try
                                {
                                    var client = new RestClient(xServerURL);
                                    var allActiveXServersRequest = new RestRequest("/getactivexservers/", Method.GET);
                                    allActiveXServersRequest.AddParameter("fromId", lastId);
                                    var allActiveXServersResult = await client.ExecuteAsync<List<ServerRegisterResult>>(allActiveXServersRequest, cancellationToken).ConfigureAwait(false);
                                    if (allActiveXServersResult.StatusCode == HttpStatusCode.OK)
                                    {
                                        var activeXServersList = allActiveXServersResult.Data;
                                        if (activeXServersList.Count() == 0)
                                        {
                                            xServerSyncComplete = true;
                                        }
                                        else
                                        {
                                            foreach (var serverResult in activeXServersList)
                                            {
                                                if (serverResult.Id > lastId)
                                                {
                                                    lastId = serverResult.Id;
                                                }
                                                if (serverResult.ProfileName == selfProfile)
                                                {
                                                    foundSelf = true;
                                                }
                                                var registeredServer = serverNodes.Where(s => s.Signature == serverResult.Signature).FirstOrDefault();
                                                if (registeredServer == null)
                                                {
                                                    var newServer = new ServerNodeData()
                                                    {
                                                        ProfileName = serverResult.ProfileName,
                                                        NetworkAddress = serverResult.NetworkAddress,
                                                        NetworkPort = serverResult.NetworkPort,
                                                        NetworkProtocol = serverResult.NetworkProtocol,
                                                        Signature = serverResult.Signature,
                                                        Tier = serverResult.Tier,
                                                        FeeAddress = serverResult.FeeAddress,
                                                        SignAddress = serverResult.SignAddress,
                                                        KeyAddress = serverResult.KeyAddress,
                                                        Active = true,
                                                        DateAdded = DateTime.UtcNow,
                                                        LastSeen = DateTime.UtcNow,
                                                        Priority = 0,
                                                        Relayed = true
                                                    };

                                                    // Local Registration of new nodes we don't know about.
                                                    await networkFeatures.Register(newServer);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        xServerSyncComplete = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.LogDebug($"Server failed to request during reco", ex);
                                }
                            }
                            if (!foundSelf)
                            {
                                result.Add(xServerURL);
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
