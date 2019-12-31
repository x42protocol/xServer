using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using X42.Feature.X42Client.RestClient.Responses;
using X42.Feature.X42Client.Utils.Web;
using X42.Utilities;

namespace X42.Feature.X42Client.RestClient
{
    public partial class X42RestClient : ApiClient
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        public X42RestClient(string baseUrl, ILogger mainLogger) : base(baseUrl, mainLogger)
        {
            logger = mainLogger;
        }

        /// <summary>
        ///     Gets Status Information For The Target Node
        /// </summary>
        public async Task<NodeStatusResponse> GetNodeStatus()
        {
            try
            {
                NodeStatusResponse response = await base.SendGet<NodeStatusResponse>("api/Node/status");

                Guard.Null(response, nameof(response), "'api/Node/status' API Response Was Null!");

                logger.LogDebug("Got Node Status Response!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"An Error '{ex.Message}' Occured When Getting The Node Status!", ex);

                return null;
            }
        }

        /// <summary>
        /// Gets an unspent transaction
        /// </summary>
        /// <param name="txid">The transaction id</param>
        /// <param name="vout">The vout of the transaction</param>
        /// <param name="includeMemPool">Whether or not to include the mempool</param>
        /// <returns>The unspent transaction for the specified transaction and vout</returns>
        public async Task<GetTXOutResponse> GetTXOut(string txid, string vout, string includeMemPool = "false")
        {
            try
            {
                GetTXOutResponse response = await base.SendGet<GetTXOutResponse>
                    ($"api/Node/gettxout?trxid={txid}&vout={vout}&includeMemPool={includeMemPool}");

                Guard.Null(response, nameof(response), "'api/Node/gettxout' API Response Was Null!");

                logger.LogDebug("Got Node Status Response!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"An Error '{ex.Message}' Occured When Getting The Node Status!", ex);

                return null;
            }
        }
    }
}