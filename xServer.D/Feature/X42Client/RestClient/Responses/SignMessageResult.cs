namespace x42.Feature.X42Client.RestClient.Responses
{
    /// <summary>
    /// Class containing details of a signature message.
    /// </summary>
    public class SignMessageResult
    {
        public string SignedAddress { get; set; }

        public string Signature { get; set; }
    }
}
