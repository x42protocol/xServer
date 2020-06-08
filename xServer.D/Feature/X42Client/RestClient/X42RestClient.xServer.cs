using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using x42.Feature.X42Client.RestClient.Responses;
using x42.Utilities;

namespace x42.Feature.X42Client.RestClient
{
    public partial class X42RestClient
    {
        /// <summary>
        ///     Retrieves the xServer stats
        /// </summary>
        public async Task<GetXServerStatsResult> GetXServerStats()
        {
            try
            {
                var response = await base.SendGet<GetXServerStatsResult>($"api/xServer/getxserverstats");

                Guard.Null(response, nameof(response), "'api/xServer/getxserverstats' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"An Error '{ex.Message}' Occured When xServer Stats!", ex);
                return null;
            }
        }
    }
}
