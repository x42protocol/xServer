namespace x42.Feature.X42Client.RestClient.Requests
{
    /// <summary>
    /// A class containing the necessary parameters for a block search request.
    /// </summary>
    public class DecodeRawTransactionRequest
    {
        /// <summary>The transaction to be decoded in hex format.</summary>
        public string RawHex { get; set; }
    }
}