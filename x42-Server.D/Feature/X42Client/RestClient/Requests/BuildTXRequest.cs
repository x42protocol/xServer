namespace X42.Feature.X42Client.RestClient.Requests
{
    public class BuildTXRequest
    {
        public string feeAmount { get; set; } = "0";
        public string password { get; set; }
        public string walletName { get; set; }
        public string accountName { get; set; }
        public x42Recipient[] recipients { get; set; }
        public bool allowUnconfirmed { get; set; } = false;
        public bool shuffleOutputs { get; set; } = true;
    }

    public class x42Recipient
    {
        public string destinationAddress { get; set; }
        public string amount { get; set; }
    }
}