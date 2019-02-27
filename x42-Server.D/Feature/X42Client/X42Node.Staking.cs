using Microsoft.Extensions.Logging;
using X42.Feature.X42Client.RestClient.Responses;
using X42.Feature.X42Client.Utils.Extensions;

namespace X42.Feature.X42Client
{
    public sealed partial class X42Node
    {
        /// <summary>
        ///     Refreshes Staking Information
        /// </summary>
        public async void UpdateStakingInformation()
        {
            GetStakingInfoResponse stakingInfo = await restClient.GetStakingInfo();
            if (stakingInfo == null)
            {
                logger.LogDebug(
                    $"Node '{Name}' ({Address}:{Port}), An Error Occured When Getting Staking Information!");
            }
            else
            {
                NetworkDifficulty = stakingInfo.difficulty;
                IsStaking = stakingInfo.staking;
                NetworkStakingWeight = stakingInfo.netStakeWeight;
                NodeStakingWeight = stakingInfo.weight.ParseApiAmount();
                ExpectedStakingTimeMins = stakingInfo.expectedTime / 60; //time is in seconds
            } //end of if (stakingInfo == null)
        } //end of public async void UpdateStakingInformation()
    } //end of X42Node.Staking
}