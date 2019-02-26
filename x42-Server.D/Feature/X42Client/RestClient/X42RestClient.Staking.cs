using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using X42.Feature.X42Client.RestClient.Responses;
using X42.Utilities;

namespace X42.Feature.X42Client.RestClient
{
    public partial class X42RestClient
    {
        /// <summary>
        ///     Gets The Node Staking Information
        /// </summary>
        public async Task<GetStakingInfoResponse> GetStakingInfo()
        {
            try
            {
                GetStakingInfoResponse response =
                    await base.SendGet<GetStakingInfoResponse>("api/Staking/getstakinginfo");

                Guard.Null(response, nameof(response), "'api/Staking/getstakinginfo' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical($"An Error '{ex.Message}' Occured When Getting Staking Info!", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<GetStakingInfoResponse> GetStakingInfo()
    } //end of class
}