using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using x42.Utilities;

namespace x42.Feature.X42Client.RestClient
{
    public partial class X42RestClient
    {
        /// <summary>
        ///     Gets The Block Hash At A Given Height
        /// </summary>
        /// <param name="height">Height of Block</param>
        public async Task<string> GetBlockHash(ulong height)
        {
            try
            {
                string response = await base.SendGet<string>($"/api/Consensus/getblockhash?height={height}");

                Guard.Null(response, nameof(response), "'/api/Consensus/getblockhash' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical($"An Error '{ex.Message}' Occured When Getting Block Hash!", ex);
                throw;
            } //end of try-catch
        } //end of public async Task<string> GetBlockHash(ulong height)


        /// <summary>
        ///     Gets The Best Block Hash
        /// </summary>
        public async Task<string> GetBestBlockHash()
        {
            try
            {
                string response = await base.SendGet<string>("/api/Consensus/getbestblockhash");

                Guard.Null(response, nameof(response), "'/api/Consensus/getbestblockhash' API Response Was Null!");

                return response;
            }
            catch (Exception ex)
            {
                logger.LogCritical($"An Error '{ex.Message}' Occured When Getting Best Block Hash!", ex);
                throw;
            }
        }
    }
}