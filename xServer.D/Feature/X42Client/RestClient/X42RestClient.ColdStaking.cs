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
        ///     Gets Cold Staking Address
        /// </summary>
        public async Task<GetColdStakingAddressResponse> GetColdStakingAddress(string walletName, bool isColdWalletAddress, bool segwit)
        {
            try
            {
                GetColdStakingAddressResponse response =
                    await base.SendGet<GetColdStakingAddressResponse>(
                        $"api/coldstaking/cold-staking-address?walletName={walletName}&Segwit={segwit}&isColdWalletAddress={isColdWalletAddress}"
                        );

                Guard.Null(response, nameof(response), "'api/Staking/getstakinginfo' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogDebug($"An Error '{ex.Message}' Occured When Getting Staking Info!", ex);
                return null;
            }
        }
    }
}
