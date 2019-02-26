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
        ///     Gets The Block Details For The Given 'Block' Hash
        /// </summary>
        /// <param name="blockHash">Hash of The Block</param>
        /// <param name="showTX">Show TX Information</param>
        /// <returns></returns>
        public async Task<GetBlockResponse> GetBlock(string blockHash, bool showTX = true)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(blockHash))
                {
                    throw new ArgumentNullException(nameof(blockHash), "Block Hash Cannot Be NULL/Empty!");
                }

                GetBlockResponse response = await base.SendGet<GetBlockResponse>(
                    $"api/BlockStore/block?Hash={blockHash}&ShowTransactionDetails={showTX}&OutputJson=true");
                Guard.Null(response, nameof(response),
                    $"NULL Data Returned For The api/BlockStore/block Call With Block Hash '{blockHash}'");


                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical($"An Error '{ex.Message}' Occured When Getting Block Information For Hash '{blockHash}'!", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<GetStakingInfoResponse> GetStakingInfo()


        /// <summary>
        ///     Gets The Block Details For The Given 'Block' Height (Makes 2 API Calls)
        /// </summary>
        /// <param name="blockHeight">Height of Target Block</param>
        /// <param name="showTX">Show TX Information</param>
        /// <returns></returns>
        public async Task<GetBlockResponse> GetBlock(ulong blockHeight, bool showTX = true)
        {
            try
            {
                string blockHash = await GetBlockHash(blockHeight);
                if (string.IsNullOrWhiteSpace(blockHash))
                {
                    throw new Exception($"An Error Occured When Attempting To Get Block Hash At Height {blockHeight}");
                }

                return await GetBlock(blockHash, showTX);
            }
            catch (Exception ex)
            {
                logger.LogCritical($"An Error '{ex.Message}' Occured When Getting Block Information At Height '{blockHeight}'!", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<GetBlockResponse> GetBlock(ulong blockHeight, bool showTX = true)


        /// <summary>
        ///     Gets The Total Number of Blocks Downloaded
        /// </summary>
        /// <returns></returns>
        public async Task<ulong> GetBlockCount()
        {
            try
            {
                return await base.SendGet<ulong>("api/BlockStore/getblockcount");
            }
            catch (Exception ex)
            {
                logger.LogCritical($"An Error '{ex.Message}' Occured When Getting Block Height'!", ex);
                throw;
            } //end of try-catch
        } //end of  public async Task<ulong> GetBlockCount()
    } //end of class
}