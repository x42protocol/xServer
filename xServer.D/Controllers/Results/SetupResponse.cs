namespace x42.Controllers.Results
{
    /// <summary>
    /// The response data structure received by a client after requesting to setup the xServer.
    /// Refer to <see cref="SetupResponse"/>.
    /// </summary>
    public class SetupResponse
    {
        /// <summary>A Base58 address from the wallet used to sign.</summary>
        public string SignAddress { get; set; }
    }
}