using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using x42.Configuration.Logging;
using x42.Feature.Database.Context;
using x42.Feature.Database;
using x42.Feature.Setup;
using x42.Feature.X42Client;
using x42.ServerNode;
using x42.Server;
using x42.Utilities;
using x42.Feature.Database.Tables;
using System.Linq;
using x42.Feature.X42Client.RestClient.Responses;
using x42.Configuration;
using Microsoft.EntityFrameworkCore;
using NBitcoin;
using System.Net.Sockets;
using System;
using RestSharp;
using System.Net;
using x42.Controllers.Results;
using x42.Server.Results;
using System.Collections.Generic;
using x42.Feature.X42Client.Enums;
using x42.Feature.X42Client.RestClient.Requests;

namespace x42.Feature.Network
{
    /// <inheritdoc />
    /// <summary>
    ///     Provides an ability to communicate with different network types.
    /// </summary>
    public class NetworkFeatures : ServerFeature
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime serverLifetime;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        private readonly ServerNodeBase network;
        private readonly DatabaseSettings databaseSettings;
        private readonly ServerSettings nodeSettings;
        private NetworkMonitor networkMonitor;
        private X42ClientSettings x42ClientSettings;
        private readonly X42ClientFeature x42FullNode;
        private readonly DatabaseFeatures database;
        private X42Node x42Client;
        private CachedServerInfo cachedServerInfo;

        public NetworkFeatures(
            ServerNodeBase network,
            ServerSettings nodeSettings,
            ILoggerFactory loggerFactory,
            DatabaseSettings databaseSettings,
            X42ClientSettings x42ClientSettings,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory,
            X42ClientFeature x42FullNode,
            DatabaseFeatures database
            )
        {
            this.network = network;
            this.nodeSettings = nodeSettings;
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.databaseSettings = databaseSettings;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
            this.x42ClientSettings = x42ClientSettings;
            this.x42FullNode = x42FullNode;
            this.database = database;

            cachedServerInfo = new CachedServerInfo();

            x42Client = new X42Node(x42ClientSettings.Name, x42ClientSettings.Address, x42ClientSettings.Port, logger, serverLifetime, asyncLoopFactory, false);
        }

        public ulong BestBlockHeight { get => x42FullNode.BlockTIP; }

        /// <summary>
        ///     Prints command-line help.
        /// </summary>
        /// <param name="serverNodeBase">The servernode to extract values from.</param>
        public static void PrintHelp(ServerNodeBase serverNodeBase)
        {
            NetworkSettings.PrintHelp(serverNodeBase);
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, ServerNodeBase network)
        {
            NetworkSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <summary>
        ///     Connect to the network.
        /// </summary>
        public void Connect(CachedServerInfo cachedWalletInfo)
        {
            logger.LogInformation("Connecting to network");
            this.cachedServerInfo = cachedWalletInfo;
            this.cachedServerInfo.FeeAddress = GetFeeAddressFromSignAddress(cachedWalletInfo.SignAddress);
            logger.LogInformation("Network connected");
        }

        public void Disconnect()
        {
            logger.LogInformation("Disconnected from network");
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            networkMonitor = new NetworkMonitor(logger, serverLifetime, asyncLoopFactory, databaseSettings, this, network);

            networkMonitor.Start();

            logger.LogInformation("Network Initialized");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Disconnect();
        }

        /// <inheritdoc />
        public override void ValidateDependencies(IServerServiceProvider services)
        {
            if (string.IsNullOrEmpty(x42ClientSettings.Name))
            {
                throw new ConfigurationException("x42Client Name setting must be set.");
            }

            if (x42ClientSettings.Port <= 0)
            {
                throw new ConfigurationException("x42Client Port setting must be set.");
            }

            if (x42ClientSettings.Address.AddressFamily == AddressFamily.Unknown)
            {
                throw new ConfigurationException("x42Client Address setting must be set, and a valid IP address.");
            }
        }

        public async Task<bool> IsServerKeyValid(ServerNodeData serverNode)
        {
            string serverKey = $"{serverNode.NetworkAddress}{serverNode.NetworkPort}{serverNode.KeyAddress}{serverNode.SignAddress}{serverNode.FeeAddress}{serverNode.Tier}{serverNode.ProfileName}";
            string profileKeyAddress = GetKeyAddressFromProfileName(serverNode.ProfileName);

            return await x42Client.VerifyMessageAsync(profileKeyAddress, serverKey, serverNode.Signature);
        }

        public async Task<string> SignPriceLock(string priceLock)
        {
            var signRequest = new SignMessageRequest()
            {
                AccountName = cachedServerInfo.AccountName,
                ExternalAddress = cachedServerInfo.SignAddress,
                Password = cachedServerInfo.Password,
                WalletName = cachedServerInfo.WalletName,
                Message = priceLock
            };
            var signMessageResult = await x42Client.SignMessageAsync(signRequest);
            return signMessageResult.Signature;
        }

        public async Task<bool> VerifySenderPriceLockSignature(string address, string priceLockId, string signature)
        {
            var valid = await x42Client.VerifyMessageAsync(address, priceLockId, signature);
            return valid;
        }

        public string GetMySignAddress()
        {
            return cachedServerInfo.SignAddress;
        }

        public string GetMyFeeAddress()
        {
            return cachedServerInfo.FeeAddress;
        }

        private string GetProtocolString(int networkProtocol)
        {
            string result = string.Empty;
            switch (networkProtocol)
            {
                case 0: // Default to HTTP
                case 1:
                    result = "http";
                    break;
                case 2:
                    result = "https";
                    break;
                case 3:
                    // TODO: Add Websocket
                    result = "ws";
                    break;
            }
            return result;
        }

        public string GetServerUrl(int networkProtocol, string networkAddress, long networkPort)
        {
            return $"{GetProtocolString(networkProtocol)}://{networkAddress}:{networkPort}/";
        }

        public bool ValidateNodeOnline(string networkAddress)
        {
            bool result = false;
            try
            {
                using var client = new TcpClient(networkAddress, Convert.ToInt32(network.DefaultNodePort));
                result = true;
            }
            catch (SocketException) { }
            return result;
        }

        public async Task<bool> ValidateServerIsOnlineAndSynced(string xServerURL)
        {
            bool result = false;
            try
            {
                logger.LogDebug($"Attempting validate connection to {xServerURL}.");

                var client = new RestClient(xServerURL);
                var xServersPingRequest = new RestRequest("/ping", Method.GET);
                var xServerPingResult = await client.ExecuteAsync<PingResult>(xServersPingRequest).ConfigureAwait(false);
                if (xServerPingResult.StatusCode == HttpStatusCode.OK)
                {
                    ulong minimumBlockHeight = xServerPingResult.Data.BestBlockHeight + network.BlockGracePeriod;
                    if (minimumBlockHeight >= BestBlockHeight)
                    {
                        result = true;
                    }
                }
            }
            catch (Exception) { }
            return result;
        }

        public async Task<Money> GetServerCollateral(ServerNodeData serverNode)
        {
            string profileKeyAddress = GetKeyAddressFromProfileName(serverNode.ProfileName);
            GetAddressesBalancesResponse addressBalance = await x42Client.GetAddressBalances(profileKeyAddress);

            if (addressBalance.balances.Count() == 1 && addressBalance.balances.FirstOrDefault().address == profileKeyAddress)
            {
                return Money.FromUnit(addressBalance.balances.FirstOrDefault().balance, MoneyUnit.Satoshi);
            }

            return Money.Zero;
        }

        public async Task<RawTransactionResponse> GetRawTransaction(string txid, bool verbose)
        {
            RawTransactionResponse rawTranscation = await x42Client.GetRawTransaction(txid, verbose);
            return rawTranscation;
        }

        public async Task<RawTransactionResponse> DecodeRawTransaction(string rawHex)
        {
            RawTransactionResponse rawTranscation = await x42Client.DecodeRawTransaction(rawHex);
            return rawTranscation;
        }

        public string GetKeyAddressFromProfileName(string profileName)
        {
            string profileKeyAddress = string.Empty;
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<ProfileData> serverNodes = dbContext.Profiles.Where(s => s.Name == profileName);
                if (serverNodes.Count() > 0)
                {
                    profileKeyAddress = serverNodes.First().KeyAddress;
                }
            }
            return profileKeyAddress;
        }

        private string GetFeeAddressFromSignAddress(string signAddress)
        {
            string feeAddress = string.Empty;
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<ServerNodeData> serverNodes = dbContext.ServerNodes.Where(s => s.SignAddress == signAddress);
                if (serverNodes.Count() > 0)
                {
                    feeAddress = serverNodes.First().FeeAddress;
                }
            }
            return feeAddress;
        }

        public async Task<string> GetServerAddress(string walletName)
        {
            var getColdStakingAddressResponse = await x42Client.GetColdStakingAddress(walletName, false, false);
            return getColdStakingAddressResponse.Address;
        }

        public async Task<GetXServerStatsResult> GetXServerStats()
        {
            var getXServerStatsResponse = await x42Client.GetXServerStats();
            return getXServerStatsResponse;
        }

        public bool IsServerReady()
        {
            return x42FullNode.Status == ConnectionStatus.Online && database.DatabaseConnected;
        }

        public async Task<RegisterResult> Register(ServerNodeData serverNode, bool serverCheckOnly = false)
        {
            RegisterResult registerResult = new RegisterResult
            {
                Success = false
            };

            if (IsNetworkAddressAllowed(serverNode.NetworkAddress))
            {
                if ((IsServerReady() && !ServerExists(serverNode)) || serverCheckOnly)
                {
                    var collateral = await GetServerCollateral(serverNode);
                    IEnumerable<Tier> availableTiers = nodeSettings.ServerNode.Tiers.Where(t => t.Collateral.Amount <= collateral);
                    Tier serverTier = availableTiers.Where(t => t.Level == (Tier.TierLevel)serverNode.Tier).FirstOrDefault();

                    if (serverTier != null)
                    {
                        bool serverKeysAreValid = await IsServerKeyValid(serverNode);
                        if (serverKeysAreValid)
                        {
                            string xServerURL = GetServerUrl(serverNode.NetworkProtocol, serverNode.NetworkAddress, serverNode.NetworkPort);
                            bool nodeAvailable = ValidateNodeOnline(serverNode.NetworkAddress);
                            if (nodeAvailable)
                            {
                                bool serverAvailable = await ValidateServerIsOnlineAndSynced(xServerURL);
                                if (serverAvailable)
                                {
                                    if (serverCheckOnly)
                                    {
                                        registerResult.Success = true;
                                    }
                                    else
                                    {
                                        bool serverAdded = AddServer(serverNode);
                                        if (!serverAdded)
                                        {
                                            registerResult.ResultMessage = "Server could not be added.";
                                        }
                                        else
                                        {
                                            registerResult.Success = true;
                                        }
                                    }
                                }
                                else
                                {
                                    registerResult.ResultMessage = "Network availability failed for xServer";
                                }
                            }
                            else
                            {
                                registerResult.ResultMessage = "Network availability failed for x42 node";
                            }
                        }
                        else
                        {
                            registerResult.ResultMessage = "Could not verify server keys";
                        }
                    }
                    else if (serverTier == null || availableTiers.Count() != 1)
                    {
                        registerResult.ResultMessage = "Requested Tier is not available or collateral amount is invalid.";
                    }
                }
                else
                {
                    if (x42FullNode.Status != ConnectionStatus.Online)
                    {
                        registerResult.ResultMessage = "Node is offline";
                    }
                    else if (!database.DatabaseConnected)
                    {
                        registerResult.ResultMessage = "Databse is offline";
                    }
                    else
                    {
                        registerResult.ResultMessage = "Already added";
                    }
                }
            }
            else
            {
                registerResult.ResultMessage = "Network address is not allowed.";
            }

            return registerResult;
        }

        /// <summary>
        ///     Add a server to the repo
        /// </summary>
        /// <param name="serverNodeData">Server Node Data.</param>
        /// <returns>Will return true if new record wad created, otherwise false.</returns>
        public bool AddServer(ServerNodeData serverNodeData)
        {
            bool result = false;

            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<ServerNodeData> serverNodes = dbContext.ServerNodes.Where(s => s.Signature == serverNodeData.Signature);
                if (serverNodes.Count() == 0)
                {
                    serverNodeData.DateAdded = DateTime.UtcNow;
                    serverNodeData.LastSeen = DateTime.UtcNow;
                    var newRecord = dbContext.Add(serverNodeData);
                    if (newRecord.State == EntityState.Added)
                    {
                        dbContext.SaveChanges();
                        result = true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Check if server is already added
        /// </summary>
        /// <param name="serverNodeData">Server Node Data.</param>
        /// <returns>Will return true if the server exists, otherwise false.</returns>
        public bool ServerExists(ServerNodeData serverNodeData)
        {
            bool result = false;

            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<ServerNodeData> serverNodes = dbContext.ServerNodes.Where(s => s.Signature == serverNodeData.Signature);
                if (serverNodes.Count() > 0)
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        ///     The key address <see cref="string" /> if key address is available, if not empty string is returned.
        /// </summary>
        public string GetServerProfile()
        {
            string activeServerProfile = string.Empty;

            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<ServerData> server = dbContext.Servers;
                if (server.Count() > 0)
                {
                    string profileName = server.First().ProfileName;

                    IQueryable<ServerNodeData> serverNode = dbContext.ServerNodes.Where(s => s.ProfileName == profileName && s.Active);
                    if (serverNode.Count() > 0)
                    {
                        activeServerProfile = profileName;
                    }
                }
            }
            return activeServerProfile;
        }

        /// <summary>
        ///     Will get this xServer <see cref="ServerNodeData" /> if key address is available, if not empty <see cref="ServerNodeData" /> is returned.
        /// </summary>
        public ServerNodeData GetSelfServerNode()
        {
            ServerNodeData result = new ServerNodeData();
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                IQueryable<ServerData> server = dbContext.Servers;
                if (server.Count() > 0)
                {
                    string profileName = server.First().ProfileName;

                    IQueryable<ServerNodeData> serverNode = dbContext.ServerNodes.Where(s => s.ProfileName == profileName && s.Active);
                    if (serverNode.Count() > 0)
                    {
                        result = serverNode.First();
                    }
                }
            }
            return result;
        }

        public bool IsNetworkAddressAllowed(string networkAddress)
        {
            if (networkAddress.StartsWith("127"))
            {
                return false;
            }
            List<string> blackListedAddresses = new List<string>
            {
                    "localhost",
                    "::1",
                    "::",
                    "0.0.0.0",
                    "0:0:0:0:0:0:0:0"
            };

            if (blackListedAddresses.Where(a => a == networkAddress).Count() > 0)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="NetworkFeatures" />.
    /// </summary>
    public static class NetworkBuilderExtension
    {
        /// <summary>
        ///     Adds network components to the server.
        /// </summary>
        /// <param name="serverBuilder">The object used to build the current node.</param>
        /// <returns>The server builder, enriched with the new component.</returns>
        public static IServerBuilder UseNetwork(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<NetworkFeatures>("network");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<NetworkFeatures>()
                    .DependOn<DatabaseFeatures>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<NetworkFeatures>();
                        services.AddSingleton<NetworkSettings>();
                    });
            });

            return serverBuilder;
        }
    }
}
