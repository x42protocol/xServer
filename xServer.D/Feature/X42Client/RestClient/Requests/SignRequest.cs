namespace x42.Feature.X42Client.RestClient.Requests
{
    /// <summary>
    /// Object to sign a message.
    /// </summary>
    public class SignMessageRequest
    {
        public string WalletName { get; set; }

        public string Password { get; set; }

        public string AccountName { get; set; }

        public string ExternalAddress { get; set; }

        public string Message { get; set; }
    }
}