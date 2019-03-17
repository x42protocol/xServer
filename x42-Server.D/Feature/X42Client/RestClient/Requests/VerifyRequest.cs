namespace X42.Feature.X42Client.RestClient.Requests
{
    /// <summary>
    /// Object to verify a signed message.
    /// </summary>
    public class VerifyRequest
    {
        public string signature { get; set; }
        public string externalAddress { get; set; }
        public string message { get; set; }
    }
}