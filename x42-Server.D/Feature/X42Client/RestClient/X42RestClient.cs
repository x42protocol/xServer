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
                logger.LogCritical($"An Error '{ex.Message}' Occured When Getting The Node Status!", ex);

                throw; //pass it back up the stack? .. this seems memory intensive to me
            } //end of try-catch
        } //end of public async Task<NodeStatusResponse> GetNodeStatus()
    } //end of public class X42RestClient:APIClient
}