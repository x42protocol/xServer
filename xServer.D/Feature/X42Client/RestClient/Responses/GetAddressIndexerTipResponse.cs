using NBitcoin;

namespace x42.Feature.X42Client.RestClient.Responses
{
    public class GetAddressIndexerTipResponse
    {
        public string tipHash { get; set; }
        public uint? tipHeight { get; set; }
    }
}