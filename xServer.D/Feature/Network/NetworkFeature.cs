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
using Newtonsoft.Json;
using x42.Controllers.Requests;
using System.Threading;
using RestSharp.Serializers.NewtonsoftJson;
using x42.Feature.X42Client.Models;
using static x42.ServerNode.Tier;
using x42.Feature.Database.Repositories.Profiles;
using x42.Feature.Database.Entities;
using x42.Feature.Database.UoW;
using Newtonsoft.Json.Serialization;

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
        private readonly NetworkSettings networkSettings;
        private readonly IProfileRepository _profileRepository;
        private readonly IUnitOfWork _unitOfWork;
        public NetworkFeatures(
            ServerNodeBase network,
            ServerSettings nodeSettings,
            ILoggerFactory loggerFactory,
            DatabaseSettings databaseSettings,
            X42ClientSettings x42ClientSettings,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory,
            X42ClientFeature x42FullNode,
            DatabaseFeatures database,
            NetworkSettings networkSettings
,
            IProfileRepository profileRepository,
            IUnitOfWork unitOfWork)
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
            this.networkSettings = networkSettings;

            cachedServerInfo = new CachedServerInfo();

            x42Client = new X42Node(x42ClientSettings.Name, x42ClientSettings.Address, x42ClientSettings.Port, logger, serverLifetime, asyncLoopFactory, false);
            _profileRepository = profileRepository;
            _unitOfWork = unitOfWork;
        }

        public uint BestBlockHeight { get => x42FullNode.BlockTIP; }
        public uint? AddressIndexerHeight { get => x42FullNode.AddressIndexterTip; }

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
            this.cachedServerInfo.PublicKey = GetPublicKeyFromSignAddress(cachedWalletInfo.SignAddress, cachedWalletInfo.WalletName, cachedWalletInfo.AccountName);
            logger.LogInformation("Network connected");
        }

        public void Disconnect()
        {
            logger.LogInformation("Disconnected from network");
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            networkMonitor = new NetworkMonitor(logger, serverLifetime, asyncLoopFactory, databaseSettings, this, network, this.networkSettings);

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

        public async Task<bool> IsProfileKeyValid(string name, string keyAddress, string returnAddress, string signature)
        {
            string profileKey = $"{name}{returnAddress}";
            return await x42Client.VerifyMessageAsync(keyAddress, profileKey, signature);
        }

        public async Task<bool> IsRegisterKeyValid(string name, string keyAddress, string returnAddress, string signature)
        {
            string profileKey = $"{name}{returnAddress}";
            return await x42Client.VerifyMessageAsync(keyAddress, profileKey, signature);
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

        public void SetProfileHeightOnSelf(int height)
        {
            database.dataStore.SetDictionaryValue("ProfileHeight", height);
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

        public async Task<BlockchainInfoResponse> GetBlockchainInfo()
        {
            BlockchainInfoResponse blockchainInfoResponse = await x42Client.GetBlockchainInfo();
            return blockchainInfoResponse;
        }

        public async Task<GetAddressIndexerTipResponse> GetAddressIndexerTip()
        {
            GetAddressIndexerTipResponse addressIndexerTipResponse = await x42Client.GetAddressIndexerTip();
            return addressIndexerTipResponse;
        }

        public string GetMyFeeAddress()
        {
            return cachedServerInfo.FeeAddress;
        }

        public string GetPublicKey()
        {
            return cachedServerInfo.PublicKey;
        }

        public async Task RelayPriceLock(PriceLockData priceLockData, List<ServerNodeData> activeXServers, CancellationToken cancellationToken)
        {
            foreach (var activeServer in activeXServers)
            {
                try
                {
                    string xServerURL = GetServerUrl(activeServer.NetworkProtocol, activeServer.NetworkAddress, activeServer.NetworkPort);
                    var client = new RestClient(xServerURL);
                    var registerRestRequest = new RestRequest("/updatepricelock", Method.Post);

                    await client.ExecuteAsync(registerRestRequest, cancellationToken);
                }
                catch (Exception) { }
            }
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
                var xServersPingRequest = new RestRequest("/ping", Method.Get);
                var xServerPingResult = await client.ExecuteAsync<PingResult>(xServersPingRequest).ConfigureAwait(false);
                if (xServerPingResult.StatusCode == HttpStatusCode.OK)
                {
                    ulong minimumBlockHeight = (xServerPingResult.Data.BestBlockHeight ?? 0) + network.BlockGracePeriod;
                    if (minimumBlockHeight >= BestBlockHeight)
                    {
                        result = true;
                    }
                }
            }
            catch (Exception) { }
            return result;
        }

        public async Task<Money> GetServerCollateral(ServerNodeData serverNode, uint blockGracePeriod)
        {
            string profileKeyAddress = GetKeyAddressFromProfileName(serverNode.ProfileName);
            if (!string.IsNullOrEmpty(profileKeyAddress))
            {
                GetAddressesBalancesResponse addressBalance = await x42Client.GetAddressBalances(profileKeyAddress, (int)blockGracePeriod);

                if (addressBalance.balances.Count() == 1 && addressBalance.balances.FirstOrDefault().address == profileKeyAddress)
                {
                    return Money.FromUnit(addressBalance.balances.FirstOrDefault().balance, MoneyUnit.Satoshi);
                }
            }
            return Money.Zero;
        }

        public async Task<PriceLockResult> GetPriceLockFromT3(CancellationToken cancellationToken, string priceLockId, bool onlyConfirmed = false)
        {
            PriceLockResult result = null;
            var tierThreeServerConnections = GetAllTier3ConnectionInfo();
            foreach (var xServerConnectionInfo in tierThreeServerConnections)
            {
                try
                {
                    string xServerURL = GetServerUrl(xServerConnectionInfo.NetworkProtocol, xServerConnectionInfo.NetworkAddress, xServerConnectionInfo.NetworkPort);
                    logger.LogDebug($"Attempting validate connection to {xServerURL}.");

                    var client = new RestClient(xServerURL);
                    var getPriceLockRequest = new RestRequest("/getpricelock", Method.Get);
                    getPriceLockRequest.AddParameter("priceLockId", priceLockId);
                    var priceLockResult = await client.ExecuteAsync<PriceLockResult>(getPriceLockRequest, cancellationToken).ConfigureAwait(false);
                    if (priceLockResult.StatusCode == HttpStatusCode.OK)
                    {
                        if (priceLockResult.Data.Success)
                        {
                            if (onlyConfirmed)
                            {
                                if (priceLockResult.Data.Status >= (int)PriceLock.Status.Confirmed)
                                {
                                    return priceLockResult.Data;
                                }
                            }
                            else
                            {
                                return priceLockResult.Data;
                            }
                        }
                    }
                }
                catch (Exception) { }
            }
            return result;
        }

        public PriceLockData GetPriceLockData(Guid priceLockId)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.PriceLocks.Where(p => p.PriceLockId == priceLockId).FirstOrDefault();
            }
        }

        public async Task SyncProfiles(CancellationToken cancellationToken)
        {
            int newHeight = 0;
            var profileHeight = database.dataStore.GetIntFromDictionary("ProfileHeight");
            var t2Servers = GetAllTier2ConnectionInfo();
            foreach (var server in t2Servers)
            {
                var profiles = await GetProfiles(cancellationToken, server, profileHeight);
                while (profiles.Count > 0)
                {
                    foreach (var profile in profiles)
                    {
                        if (profile.BlockConfirmed > newHeight)
                        {
                            newHeight = profile.BlockConfirmed;
                        }
                        var newProfile = new ProfileData()
                        {
                            KeyAddress = profile.KeyAddress,
                            Name = profile.Name,
                            ReturnAddress = profile.ReturnAddress,
                            Signature = profile.Signature,
                            BlockConfirmed = profile.BlockConfirmed,
                            PriceLockId = profile.PriceLockId,
                            Relayed = true,
                            Status = (int)Profile.Status.Created
                        };
                        if (!ProfileExists(newProfile.Name, newProfile.KeyAddress, true))
                        {
                            var priceLock = await GetPriceLockFromT3(cancellationToken, newProfile.PriceLockId, true);
                            if (priceLock != null)
                            {
                                var priceLockExists = AddCompletePriceLock(priceLock);
                                if (priceLockExists)
                                {
                                    await AddProfile(newProfile);
                                }
                            }
                        }
                    }
                    profiles = await GetProfiles(cancellationToken, server, newHeight);
                }
            }
            if (newHeight > profileHeight)
            {
                SetProfileHeightOnSelf(newHeight);
            }
        }

        private async Task<bool> AddProfile(ProfileData profileData)
        {
            bool result = false;
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                bool isProfileKeyValid = await IsProfileKeyValid(profileData.Name, profileData.KeyAddress, profileData.ReturnAddress, profileData.Signature);
                if (isProfileKeyValid)
                {
                    var newRecord = dbContext.Profiles.Add(profileData);

                    _profileRepository.Add(new XServerProfile(profileData));
                    await _unitOfWork.Commit();
                    if (newRecord.State == EntityState.Added)
                    {
                        var saved = dbContext.SaveChanges();
                        if (saved > 0)
                        {
                            result = true;

                        }
                    }
                }
            }
            return result;
        }

        public bool AddCompletePriceLock(PriceLockResult priceLockResult)
        {
            bool result = false;
            if (Guid.TryParse(priceLockResult.PriceLockId, out Guid validPriceLockId))
            {
                var priceLock = GetPriceLockData(validPriceLockId);
                if (priceLock != null)
                {
                    // TODO: Validate transaction ID, and amounts.
                    if (!string.IsNullOrEmpty(priceLock?.TransactionId))
                    {
                        return true;
                    }
                    else if (!string.IsNullOrEmpty(priceLockResult?.TransactionId))
                    {
                        using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
                        {
                            var priceLockRecord = dbContext.PriceLocks.Where(p => p.PriceLockId == validPriceLockId).FirstOrDefault();
                            priceLockRecord.DestinationAddress = priceLockResult.DestinationAddress;
                            priceLockRecord.DestinationAmount = priceLockResult.DestinationAmount;
                            priceLockRecord.ExpireBlock = priceLockResult.ExpireBlock;
                            priceLockRecord.FeeAddress = priceLockResult.FeeAddress;
                            priceLockRecord.FeeAmount = priceLockResult.FeeAmount;
                            priceLockRecord.PayeeSignature = priceLockResult.PayeeSignature;
                            priceLockRecord.PriceLockId = validPriceLockId;
                            priceLockRecord.PriceLockSignature = priceLockResult.PriceLockSignature;
                            priceLockRecord.Relayed = true;
                            priceLockRecord.RequestAmount = priceLockResult.RequestAmount;
                            priceLockRecord.RequestAmountPair = priceLockResult.RequestAmountPair;
                            priceLockRecord.SignAddress = priceLockResult.SignAddress;
                            priceLockRecord.Status = priceLockResult.Status;
                            priceLockRecord.TransactionId = priceLockResult.TransactionId;

                            var saved = dbContext.SaveChanges();
                            if (saved > 0)
                            {
                                result = true;
                            }
                        }
                    }

                }
                else
                {
                    var newPriceLock = new PriceLockData()
                    {
                        DestinationAddress = priceLockResult.DestinationAddress,
                        DestinationAmount = priceLockResult.DestinationAmount,
                        ExpireBlock = priceLockResult.ExpireBlock,
                        FeeAddress = priceLockResult.FeeAddress,
                        FeeAmount = priceLockResult.FeeAmount,
                        PayeeSignature = priceLockResult.PayeeSignature,
                        PriceLockId = validPriceLockId,
                        PriceLockSignature = priceLockResult.PriceLockSignature,
                        Relayed = true,
                        RequestAmount = priceLockResult.RequestAmount,
                        RequestAmountPair = priceLockResult.RequestAmountPair,
                        SignAddress = priceLockResult.SignAddress,
                        Status = priceLockResult.Status,
                        TransactionId = priceLockResult.TransactionId
                    };
                    using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
                    {
                        var newPriceLockRecord = dbContext.PriceLocks.Add(newPriceLock);
                        if (newPriceLockRecord.State == EntityState.Added)
                        {
                            var saved = dbContext.SaveChanges();
                            if (saved > 0)
                            {
                                if (!string.IsNullOrEmpty(newPriceLock?.TransactionId)) // TODO: Validate transaction ID, and amounts.
                                {
                                    result = true;
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        public bool ProfileExists(string name = "", string keyAddress = "", bool skipReservations = false)
        {
            int profileCount = 0;
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(keyAddress))
            {
                if (!skipReservations)
                {
                    profileCount = database.dataStore.GetProfileReservationCountSearch(name, keyAddress);
                }
                profileCount += database.dataStore.GetProfileCountSearch(name, keyAddress);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                if (!skipReservations)
                {
                    profileCount = database.dataStore.GetProfileReservationCountByName(name);
                }
                profileCount += database.dataStore.GetProfileCountByName(name);
            }
            else if (!string.IsNullOrEmpty(keyAddress))
            {
                if (!skipReservations)
                {
                    profileCount = database.dataStore.GetProfileReservationCountByKeyAddress(keyAddress);
                }
                profileCount += database.dataStore.GetProfileCountByKeyAddress(keyAddress);
            }

            return profileCount > 0;
        }

        public async Task RelayProfileReservation(CancellationToken cancellationToken, ProfileReservationData profileReservationData, XServerConnectionInfo xServerConnectionInfo)
        {
            string xServerURL = GetServerUrl(xServerConnectionInfo.NetworkProtocol, xServerConnectionInfo.NetworkAddress, xServerConnectionInfo.NetworkPort);
            try
            {
                var reserveRequest = new ReceiveProfileReserveRequest()
                {
                    KeyAddress = profileReservationData.KeyAddress,
                    Name = profileReservationData.Name,
                    ReturnAddress = profileReservationData.ReturnAddress,
                    Signature = profileReservationData.Signature,
                    PriceLockId = profileReservationData.PriceLockId,
                    ReservationExpirationBlock = profileReservationData.ReservationExpirationBlock
                };

                logger.LogDebug($"Attempting relay profile reservation to {xServerURL}.");

                var client = new RestClient(xServerURL);
                var reserveProfileRequest = new RestRequest("/receiveprofilereservation", Method.Post);

                var result = await client.ExecuteAsync<ReserveProfileResult>(reserveProfileRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Relay profile reservation failed for {xServerURL}.", ex);
            }
        }

        public async Task<List<ProfilesResult>> GetProfiles(CancellationToken cancellationToken, XServerConnectionInfo xServerConnectionInfo, int fromBlock)
        {
            var result = new List<ProfilesResult>();
            string xServerURL = GetServerUrl(xServerConnectionInfo.NetworkProtocol, xServerConnectionInfo.NetworkAddress, xServerConnectionInfo.NetworkPort);
            try
            {
                logger.LogDebug($"Attempting GetProfiles from {xServerURL}.");

                var client = new RestClient(xServerURL);
                var nextProfileRequest = new RestRequest("/getnextprofiles", Method.Get);
                nextProfileRequest.AddParameter("fromBlock", fromBlock);
                var priceLockResult = await client.ExecuteAsync<List<ProfilesResult>>(nextProfileRequest, cancellationToken).ConfigureAwait(false);
                if (priceLockResult.StatusCode == HttpStatusCode.OK)
                {
                    result = priceLockResult.Data;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug($"GetProfiles failed from {xServerURL}.", ex);
            }
            return result;
        }

        public async Task<PriceLockResult> CreateNewPriceLock(CreatePriceLockRequest priceLockRequest)
        {
            PriceLockResult result = null;
            var tierThreeServerConnections = GetAllTier3ConnectionInfo();
            tierThreeServerConnections = tierThreeServerConnections.Take(1).ToList();

            foreach (var xServerConnectionInfo in tierThreeServerConnections)
            {
                try
                {
                    string xServerURL = GetServerUrl(xServerConnectionInfo.NetworkProtocol, xServerConnectionInfo.NetworkAddress, xServerConnectionInfo.NetworkPort);
                    logger.LogDebug($"Attempting validate connection to {xServerURL}.");

                    var client = new RestClient(xServerURL);
                    var createPriceLockRequest = new RestRequest("/createpricelock", Method.Post);                 

                    createPriceLockRequest.AddBody(priceLockRequest);
 
                    var createPLResult = await client.ExecuteAsync<PriceLockResult>(createPriceLockRequest);

                    Console.WriteLine(createPLResult.ErrorMessage);

                    if (createPLResult.StatusCode == HttpStatusCode.OK)
                    {
                        return createPLResult.Data;
                    }
                }
                catch (Exception e) {


                    Console.WriteLine(e.InnerException);
                }
            }
            return result;
        }

        public List<XServerConnectionInfo> GetAllTier3ConnectionInfo(int top = 0)
        {
            var tierThreeAddresses = new List<XServerConnectionInfo>();

            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                // Remove any servers that have been unavailable past the grace period.
                List<ServerNodeData> tierThreeServers;
                if (top == 0)
                {
                    tierThreeServers = dbContext.ServerNodes.Where(s => s.Tier == (int)Tier.TierLevel.Three && s.Active).OrderBy(s => s.Priority).ToList();
                }
                else
                {
                    tierThreeServers = dbContext.ServerNodes.Where(s => s.Tier == (int)Tier.TierLevel.Three && s.Active).OrderBy(s => s.Priority).Take(top).ToList();
                }
                tierThreeAddresses = GetServerConnectionInfoList(tierThreeServers);
                if (tierThreeAddresses.Count == 0)
                {
                    var serverList = GetXServerStats().Result;
                    var tier3NodeList = serverList.Nodes.Where(n => n.Tier == (int)Tier.TierLevel.Three).ToList();
                    tierThreeAddresses = GetServerConnectionInfoList(tier3NodeList);
                }
            }
            return tierThreeAddresses;
        }

        public List<XServerConnectionInfo> GetAllTier2ConnectionInfo()
        {
            var tierTwoAddresses = new List<XServerConnectionInfo>();

            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                // Remove any servers that have been unavailable past the grace period.
                var tierTwoServers = dbContext.ServerNodes.Where(s => s.Tier == (int)Tier.TierLevel.Two && s.Active).OrderBy(s => s.Priority).ToList();
                tierTwoAddresses = GetServerConnectionInfoList(tierTwoServers);
                if (tierTwoAddresses.Count == 0)
                {
                    var serverList = GetXServerStats().Result;
                    var tier2NodeList = serverList.Nodes.Where(n => n.Tier == (int)Tier.TierLevel.Two).ToList();
                    tierTwoAddresses = GetServerConnectionInfoList(tier2NodeList);
                }
            }
            return tierTwoAddresses;
        }


        private List<XServerConnectionInfo> GetServerConnectionInfoList(List<ServerNodeData> servers)
        {
            var serverAddresses = new List<XServerConnectionInfo>();
            foreach (ServerNodeData server in servers)
            {
                var xServerConnectionInfo = new XServerConnectionInfo()
                {
                    Name = server.ProfileName,
                    NetworkAddress = server.NetworkAddress,
                    NetworkProtocol = server.NetworkProtocol,
                    NetworkPort = server.NetworkPort,
                    Priotiry = server.Priority,
                    Tier = server.Tier
                };
                serverAddresses.Add(xServerConnectionInfo);
            }
            return serverAddresses;
        }

        private List<XServerConnectionInfo> GetServerConnectionInfoList(List<xServerPeer> peers)
        {
            var serverAddresses = new List<XServerConnectionInfo>();
            foreach (var peer in peers)
            {
                var xServerConnectionInfo = new XServerConnectionInfo()
                {
                    Name = peer.Name,
                    NetworkAddress = peer.NetworkAddress,
                    NetworkProtocol = peer.NetworkProtocol,
                    NetworkPort = peer.NetworkPort,
                    Priotiry = peer.Priority,
                    Tier = peer.Tier
                };
                serverAddresses.Add(xServerConnectionInfo);
            }
            return serverAddresses;
        }

        public async Task<RawTransactionResponse> GetRawTransaction(string txid, bool verbose)
        {
            RawTransactionResponse rawTranscation = await x42Client.GetRawTransaction(txid, verbose);
            return rawTranscation;
        }

        public async Task<WalletSendTransactionModel> SendTransaction(string txhex)
        {
            WalletSendTransactionModel sentTransaction = await x42Client.SendTransaction(txhex);
            return sentTransaction;
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
        public string GetPublicKeyFromSignAddress(string address, string walletName, string accountName)
        {
            var getPublicKeyResponse = x42Client.GetPublicKey(address, walletName, accountName).Result;
            return getPublicKeyResponse;
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

        public bool IsServerConnected()
        {
            return IsNodeStatusOnline() && IsDatabaseConnected();
        }

        public bool IsNodeStatusOnline()
        {
            return x42FullNode.Status == ConnectionStatus.Online;
        }

        public bool IsDatabaseConnected()
        {
            return database.DatabaseConnected;
        }

        public bool IsServerReady()
        {
            return IsServerConnected() && networkMonitor.NetworkStartupStatus == StartupStatus.Started;
        }

        public StartupStatus GetStartupStatus()
        {
            return networkMonitor.NetworkStartupStatus;
        }

        public async Task<Tier> GetServerTier(ServerNodeData serverNode, uint blockGracePeriod)
        {
            var collateral = await GetServerCollateral(serverNode, blockGracePeriod);
            IEnumerable<Tier> availableTiers = nodeSettings.ServerNode.Tiers.Where(t => t.Collateral.Amount <= collateral);
            Tier serverTier = availableTiers.Where(t => t.Level == (Tier.TierLevel)serverNode.Tier).FirstOrDefault();
            return serverTier;
        }

        public async Task<RegisterResult> Register(ServerNodeData serverNode, bool serverCheckOnly = false, uint blockGracePeriod = 1)
        {
            RegisterResult registerResult = new RegisterResult
            {
                Success = false
            };

            if (IsNetworkAddressAllowed(serverNode.NetworkAddress))
            {
                if ((IsServerConnected() && !ServerExists(serverNode)) || serverCheckOnly)
                {
                    var serverTier = await GetServerTier(serverNode, blockGracePeriod);
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
                    else if (serverTier == null)
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
                var profileName = database.dataStore.GetStringFromDictionary("ProfileName");
                if (!string.IsNullOrEmpty(profileName))
                {
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
                var profileName = database.dataStore.GetStringFromDictionary("ProfileName");
                if (!string.IsNullOrEmpty(profileName))
                {
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
