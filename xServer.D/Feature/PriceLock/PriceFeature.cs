using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using x42.Configuration.Logging;
using x42.Feature.Database;
using x42.Feature.Setup;
using x42.ServerNode;
using x42.Server;
using x42.Utilities;
using System.Collections.Generic;
using x42.Feature.Database.Context;
using System.Linq;
using x42.Controllers.Results;
using x42.Feature.Database.Tables;
using System;
using System.Threading;
using RestSharp;
using x42.Feature.Network;
using System.Net;
using x42.Feature.PriceLock.Results;
using x42.Controllers.Requests;
using Microsoft.EntityFrameworkCore;
using x42.Utilities.Extensions;
using x42.Feature.X42Client.RestClient.Responses;

namespace x42.Feature.PriceLock
{
    /// <inheritdoc />
    /// <summary>
    ///     Provides the ability to agree on a price based on supplied pairs.
    /// </summary>
    public class PriceFeature : ServerFeature
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        ///     A cancellation token source that can cancel the node monitoring processes and is linked to the <see cref="IxServerLifetime.ApplicationStopping"/>.
        /// </summary>
        private CancellationTokenSource networkCancellationTokenSource;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime serverLifetime;

        /// <summary>Loop in which the node attempts to maintain an avrage agreed price accross the network.</summary>
        private IAsyncLoop networkPriceLoop;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory asyncLoopFactory;

        private readonly ServerNodeBase network;
        private readonly DatabaseSettings databaseSettings;
        private readonly DatabaseFeatures database;
        private readonly NetworkFeatures networkFeatures;
        private readonly XServer xServer;

        /// <summary>Time in seconds between attempts to update my x42/pair price</summary>
        private readonly int updateMyPriceSeconds = 600;

        /// <summary>Time in seconds between attempts to update network x42/pair price</summary>
        private readonly int updateNetworkPriceSeconds = 1800;

        /// <summary>Fee for the price lock. Default is 1%. TODO: Add this into configuration.</summary>
        private readonly decimal priceLockFeePercent = 1;

        public List<FiatPair> FiatPairs { get; set; } = new List<FiatPair>();
        private PriceLockValidation priceLockValidation;

        public PriceFeature(
            ServerNodeBase network,
            ILoggerFactory loggerFactory,
            DatabaseSettings databaseSettings,
            IxServerLifetime serverLifetime,
            IAsyncLoopFactory asyncLoopFactory,
            DatabaseFeatures database,
            NetworkFeatures networkFeatures,
            XServer xServer
            )
        {
            this.network = network;
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.databaseSettings = databaseSettings;
            this.serverLifetime = serverLifetime;
            this.asyncLoopFactory = asyncLoopFactory;
            this.database = database;
            this.networkFeatures = networkFeatures;
            this.xServer = xServer;
        }

        /// <inheritdoc />
        public override Task InitializeAsync()
        {
            var pairs = EnumUtil.GetValues<FiatCurrency>();
            foreach (var pair in pairs)
            {
                FiatPairs.Add(new FiatPair(pair));
            }

            PriceLockServices();
            priceLockValidation = new PriceLockValidation(networkFeatures);

            logger.LogInformation("Price Initialized");

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override void ValidateDependencies(IServerServiceProvider services)
        {
        }

        private void PriceLockServices()
        {
            this.networkCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { serverLifetime.ApplicationStopping });

            asyncLoopFactory.Run("Price.MyMonitor", async token =>
            {
                try
                {
                    if (xServer.Stats.TierLevel == Tier.TierLevel.Three)
                    {
                        await UpdateMyPriceList(this.networkCancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_MY_PRICE]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromSeconds(this.updateMyPriceSeconds),
            startAfter: TimeSpans.TenSeconds);

            asyncLoopFactory.Run("Price.NetworkMonitor", async token =>
            {
                try
                {
                    if (xServer.Stats.TierLevel == Tier.TierLevel.Three)
                    {
                        await UpdateNetworkPriceList(this.networkCancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_NETWORK_PRICE]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpan.FromSeconds(this.updateNetworkPriceSeconds),
            startAfter: TimeSpans.TenSeconds);

            asyncLoopFactory.Run("Price.PriceLockMonitor", async token =>
            {
                try
                {
                    if (xServer.Stats.TierLevel == Tier.TierLevel.Three && networkFeatures.IsServerReady())
                    {
                        await PriceLockChecks(this.networkCancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError("Exception: {0}", ex);
                    this.logger.LogTrace("(-)[UNHANDLED_EXCEPTION_NETWORK_PRICE]");
                    throw;
                }
            },
            this.networkCancellationTokenSource.Token,
            repeatEvery: TimeSpans.Minute,
            startAfter: TimeSpans.TenSeconds);
        }

        private PriceLockData GetPriceLockData(Guid priceLockId)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.PriceLocks.Where(p => p.PriceLockId == priceLockId).FirstOrDefault();
            }
        }

        public async Task<SubmitPaymentResult> SubmitPayment(SubmitPaymentRequest submitPaymentRequest)
        {
            var result = new SubmitPaymentResult();
            if (Guid.TryParse(submitPaymentRequest.PriceLockId, out Guid validPriceLockId))
            {
                var payeeValidationResult = await ValidateNewPriceLockPayee(submitPaymentRequest);
                if (payeeValidationResult == PaymentErrorCodes.None)
                {
                    using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
                    {
                        var priceLock = dbContext.PriceLocks.Where(p => p.PriceLockId == validPriceLockId).FirstOrDefault();
                        if (priceLock != null)
                        {
                            if (priceLock.Status == (int)Status.New)
                            {
                                priceLock.PayeeSignature = submitPaymentRequest.PayeeSignature;
                                priceLock.TransactionId = submitPaymentRequest.TransactionId;
                                priceLock.Status = (int)Status.WaitingForConfirmation;
                                priceLock.Relayed = false;
                                dbContext.SaveChanges();
                                if (!string.IsNullOrEmpty(submitPaymentRequest.TransactionHex))
                                {
                                    await networkFeatures.SendTransaction(submitPaymentRequest.TransactionHex);
                                }
                                result.Success = true;
                            }
                            else
                            {
                                result.ErrorCode = (int)PaymentErrorCodes.NotNew;
                            }
                        }
                        else
                        {
                            result.ErrorCode = (int)PaymentErrorCodes.PriceLockNotFound;
                        }
                    }
                }
                else
                {
                    result.ErrorCode = (int)payeeValidationResult;
                }
            }
            return result;
        }

        public async Task<PriceLockResult> CreatePriceLock(CreatePriceLockRequest priceLockRequest, string validPriceLockId = "")
        {
            var result = new PriceLockResult();
            var fiatPair = FiatPairs.Where(f => (int)f.Currency == priceLockRequest.RequestAmountPair).FirstOrDefault();
            if (fiatPair != null)
            {
                var averagePrice = fiatPair.GetPrice();
                if (averagePrice != -1)
                {
                    var price = Math.Round(priceLockRequest.RequestAmount / averagePrice, 8).Normalize();
                    var fee = Math.Round(price * priceLockFeePercent / 100, 8).Normalize();

                    var feeAddress = networkFeatures.GetMyFeeAddress();
                    var signAddress = networkFeatures.GetMySignAddress();

                    if (price < 42000000)
                    {
                        var expirationBlock = Convert.ToInt64(networkFeatures.BestBlockHeight + Convert.ToUInt64(priceLockRequest.ExpireBlock));

                        using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
                        {
                            var newPriceLock = new PriceLockData()
                            {
                                DestinationAddress = priceLockRequest.DestinationAddress,
                                DestinationAmount = price,
                                FeeAmount = fee,
                                SignAddress = signAddress,
                                FeeAddress = feeAddress,
                                ExpireBlock = expirationBlock,
                                RequestAmount = priceLockRequest.RequestAmount,
                                RequestAmountPair = priceLockRequest.RequestAmountPair,
                                Status = (int)Status.New
                            };
                            if (!string.IsNullOrEmpty(validPriceLockId))
                            {
                                newPriceLock.PriceLockId = new Guid(validPriceLockId);
                            }
                            var newPriceLockRecord = dbContext.Add(newPriceLock);
                            if (newPriceLockRecord.State == EntityState.Added)
                            {
                                string signature = await networkFeatures.SignPriceLock($"{newPriceLock.PriceLockId}{newPriceLock.DestinationAddress}{newPriceLock.DestinationAmount}{newPriceLock.FeeAddress}{newPriceLock.FeeAmount}");
                                if (!string.IsNullOrEmpty(signature))
                                {
                                    newPriceLock.PriceLockSignature = signature;
                                    dbContext.SaveChanges();

                                    result.DestinationAddress = newPriceLock.DestinationAddress;
                                    result.DestinationAmount = newPriceLock.DestinationAmount;
                                    result.FeeAddress = newPriceLock.FeeAddress;
                                    result.SignAddress = newPriceLock.SignAddress;
                                    result.FeeAmount = newPriceLock.FeeAmount;
                                    result.RequestAmount = newPriceLock.RequestAmount;
                                    result.RequestAmountPair = newPriceLock.RequestAmountPair;
                                    result.PriceLockId = newPriceLock.PriceLockId.ToString();
                                    result.PriceLockSignature = newPriceLock.PriceLockSignature;
                                    result.ExpireBlock = newPriceLock.ExpireBlock;
                                    result.Status = (int)Status.New;
                                    result.Success = true;
                                }
                                else
                                {
                                    result.ResultMessage = "Problem with node, Failed to sign price lock.";
                                    result.Success = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        result.ResultMessage = "Price not valid, max cap exceeded.";
                        result.Success = false;
                    }
                }
                else
                {
                    result.ResultMessage = "Node could not create price lock because insufficient price data.";
                    result.Success = false;
                }
            }
            else
            {
                result.ResultMessage = "The supplied pair does not exist.";
                result.Success = false;
            }
            return result;
        }

        public async Task<bool> UpdatePriceLock(PriceLockResult priceLockData)
        {
            bool result = false;
            if (Guid.TryParse(priceLockData.PriceLockId, out Guid validPriceLockId))
            {
                // Create price lock if it doesn't exist.
                var priceLock = GetPriceLockData(validPriceLockId);
                if (priceLock == null)
                {
                    var priceLockCreateRequest = new CreatePriceLockRequest()
                    {
                        DestinationAddress = priceLockData.DestinationAddress,
                        ExpireBlock = priceLockData.ExpireBlock,
                        RequestAmount = priceLockData.RequestAmount,
                        RequestAmountPair = priceLockData.RequestAmountPair
                    };
                    var createResult = await CreatePriceLock(priceLockCreateRequest, validPriceLockId.ToString());
                    if (!createResult.Success)
                    {
                        return false;
                    }
                }

                // Update payment information.
                if (!string.IsNullOrEmpty(priceLockData.TransactionID) && !string.IsNullOrEmpty(priceLockData.PayeeSignature))
                {
                    var paymentSubmit = new SubmitPaymentRequest()
                    {
                        PayeeSignature = priceLockData.PayeeSignature,
                        PriceLockId = priceLockData.PriceLockId,
                        TransactionId = priceLockData.TransactionID
                    };
                    var submitPaymentResult = await SubmitPayment(paymentSubmit);
                    if (!submitPaymentResult.Success)
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        public List<PairResult> GetPairList()
        {
            var result = new List<PairResult>();
            var pairs = EnumUtil.GetValues<FiatCurrency>();
            foreach (var pair in pairs)
            {
                var pairResult = new PairResult()
                {
                    Id = (int)pair,
                    Symbol = pair.ToString()
                };
                result.Add(pairResult);
            }
            return result;
        }

        public PriceLockResult GetPriceLock(Guid priceLockId)
        {
            var result = new PriceLockResult();
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                var priceLock = dbContext.PriceLocks.Where(p => p.PriceLockId == priceLockId).FirstOrDefault();
                if (priceLock != null)
                {
                    result.DestinationAddress = priceLock.DestinationAddress;
                    result.DestinationAmount = priceLock.DestinationAmount;
                    result.FeeAddress = priceLock.FeeAddress;
                    result.FeeAmount = priceLock.FeeAmount;
                    result.RequestAmount = priceLock.RequestAmount;
                    result.RequestAmountPair = priceLock.RequestAmountPair;
                    result.PriceLockId = priceLock.PriceLockId.ToString();
                    result.PriceLockSignature = priceLock.PriceLockSignature;
                    result.Status = priceLock.Status;
                    result.PayeeSignature = priceLock.PayeeSignature;
                    result.TransactionID = priceLock.TransactionId;
                    result.ExpireBlock = priceLock.ExpireBlock;
                    result.Success = true;
                }
                else
                {
                    result.ResultMessage = "Problem with node, Failed to sign price lock.";
                    result.Success = false;
                }
            }
            return result;
        }

        public async Task<PaymentErrorCodes> ValidateNewPriceLockPayee(SubmitPaymentRequest submitPaymentRequest)
        {
            PaymentErrorCodes result;

            var priceLockData = GetPriceLockData(new Guid(submitPaymentRequest.PriceLockId));

            if (priceLockData != null)
            {
                if (priceLockData.TransactionId == null && priceLockData.PayeeSignature == null)
                {
                    RawTransactionResponse paymentTransaction;
                    if (string.IsNullOrEmpty(submitPaymentRequest.TransactionHex))
                    {
                        paymentTransaction = await networkFeatures.GetRawTransaction(submitPaymentRequest.TransactionId, true);
                    }
                    else
                    {
                        paymentTransaction = await networkFeatures.DecodeRawTransaction(submitPaymentRequest.TransactionHex);
                    }
                    if (paymentTransaction != null)
                    {
                        bool validDestinationFound = false;
                        bool validFeeFound = false;

                        foreach (var output in paymentTransaction.VOut)
                        {
                            if (output.ScriptPubKey.Addresses.Count() == 1 && output.ScriptPubKey.Addresses[0] == priceLockData.DestinationAddress && output.Value == priceLockData.DestinationAmount)
                            {
                                validDestinationFound = true;
                            }
                            else if (output.ScriptPubKey.Addresses.Count() == 1 && output.ScriptPubKey.Addresses[0] == priceLockData.FeeAddress && output.Value == priceLockData.FeeAmount)
                            {
                                validFeeFound = true;
                            }
                        }
                        if (!validDestinationFound)
                        {
                            return PaymentErrorCodes.TransactionDestNotFound;
                        }
                        if (!validFeeFound)
                        {
                            return PaymentErrorCodes.TransactionFeeNotFound;
                        }

                        var isPayeeValid = await priceLockValidation.IsPayeeSignatureValid(paymentTransaction, submitPaymentRequest.PriceLockId, submitPaymentRequest.PayeeSignature);
                        if (isPayeeValid)
                        {
                            result = PaymentErrorCodes.None;
                        }
                        else
                        {
                            result = PaymentErrorCodes.InvalidSignature;
                        }
                    }
                    else
                    {
                        result = PaymentErrorCodes.TransactionError;
                    }
                }
                else
                {
                    result = PaymentErrorCodes.AlreadyExists;
                }
            }
            else
            {
                result = PaymentErrorCodes.PriceLockNotFound;
            }
            return result;
        }

        /// <summary>
        ///     Update my x42/pair price list
        /// </summary>
        private async Task UpdateMyPriceList(CancellationToken cancellationToken)
        {
            // TODO: Add a configuration option for the users to add thier own API to get the prices.
            foreach (var fiatPair in FiatPairs)
            {
                var coinGeckoPriceResult = await GetCoinGeckoPrice(cancellationToken, fiatPair.Currency);
                if (coinGeckoPriceResult != null)
                {
                    fiatPair.AddMyPrice(coinGeckoPriceResult.X42Protocol.Price);
                }
            }
        }

        /// <summary>
        ///     Update the network x42/pair price list
        /// </summary>
        private async Task UpdateNetworkPriceList(CancellationToken cancellationToken)
        {
            var networkPriceListSize = FiatPairs.FirstOrDefault().NetworkPriceListSize;
            var tierThreeServerConnections = networkFeatures.GetAllTier3ConnectionInfo(networkPriceListSize);
            foreach (var serverConnectionInfo in tierThreeServerConnections)
            {
                var nodePriceResults = await GetPriceFromTierThree(cancellationToken, serverConnectionInfo);
                foreach (var nodeResult in nodePriceResults)
                {
                    if (nodeResult.Price > 0)
                    {
                        var fiatPair = FiatPairs.Where(f => (int)f.Currency == nodeResult.Pair).FirstOrDefault();
                        if (fiatPair != null)
                        {
                            fiatPair.AddNetworkPrice(nodeResult.Price);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Monitor active price locks to mature state.
        /// </summary>
        private async Task PriceLockChecks(CancellationToken cancellationToken)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                var priceLocks = dbContext.PriceLocks.Where(p => p.Status > (int)Status.New && p.Status < (int)Status.Mature);
                foreach (var priceLock in priceLocks)
                {
                    try
                    {
                        var plTransaction = await networkFeatures.GetRawTransaction(priceLock.TransactionId, true);
                        if (plTransaction.Confirmations >= 500)
                        {
                            priceLock.Status = (int)Status.Mature;
                        }
                        else if (plTransaction.Confirmations >= 1)
                        {
                            priceLock.Status = (int)Status.Confirmed;
                        }
                    }
                    catch (Exception) { }
                }
                dbContext.SaveChanges();
            }
        }

        private async Task<List<PriceResult>> GetPriceFromTierThree(CancellationToken cancellationToken, XServerConnectionInfo xServerConnectionInfo)
        {
            List<PriceResult> priceResult = new List<PriceResult>();
            string xServerURL = networkFeatures.GetServerUrl(xServerConnectionInfo.NetworkProtocol, xServerConnectionInfo.NetworkAddress, xServerConnectionInfo.NetworkPort);
            var client = new RestClient(xServerURL);
            var activeServerCountRequest = new RestRequest("/getprices/", Method.GET);
            var getPriceResult = await client.ExecuteAsync<List<PriceResult>>(activeServerCountRequest, cancellationToken).ConfigureAwait(false);
            if (getPriceResult.StatusCode == HttpStatusCode.OK)
            {
                priceResult = getPriceResult.Data;
            }
            return priceResult;
        }

        private async Task<CoinGeckoPriceResult> GetCoinGeckoPrice(CancellationToken cancellationToken, FiatCurrency currency)
        {
            var client = new RestClient("https://api.coingecko.com/api/v3/simple/");
            var request = new RestRequest($"price?ids=x42-protocol&vs_currencies={currency}", Method.GET);
            var response = await client.ExecuteAsync<CoinGeckoPriceResult>(request, cancellationToken);
            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response from CoinGecko. Check inner details for more info.";
                throw new Exception(message, response.ErrorException);
            }
            return response.Data;
        }
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="PriceFeature" />.
    /// </summary>
    public static class PriceBuilderExtension
    {
        /// <summary>
        ///     Adds Price feature
        /// </summary>
        /// <param name="serverBuilder">The object used to build the current node.</param>
        /// <returns>The server builder, enriched with the new component.</returns>
        public static IServerBuilder UsePriceLock(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<PriceFeature>("price");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<PriceFeature>()
                    .DependOn<DatabaseFeatures>()
                    .DependOn<NetworkFeatures>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<PriceFeature>();
                    });
            });

            return serverBuilder;
        }
    }
}
