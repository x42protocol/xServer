namespace x42.Feature.X42Client.RestClient.Requests
{
    /// <summary>
    ///     A class containing the necessary parameters for a send transaction request.
    /// </summary>
    public class SendTransactionRequest
    {
        /// <summary>
        /// The transaction as a hexadecimal string.
        /// </summary>
        public string Hex { get; set; }
    }
}