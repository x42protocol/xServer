namespace x42.Feature.X42Client.RestClient.Responses
{
    public class GetWalletAddressesResponse
    {
        public WalletAddress[] addresses { get; set; }
    }

    public class WalletAddress
    {
        public string address { get; set; }
        public bool isUsed { get; set; }
        public bool isChange { get; set; }
    }
}