namespace X42.Feature.X42Client.RestClient.Requests
{
    public class CreateWalletRequest
    {
        public string mnemonic { get; set; }
        public string password { get; set; }
        public string passphrase { get; set; }
        public string name { get; set; }
    }
}