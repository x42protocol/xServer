namespace x42.Controllers.Results
{
    /// <summary>
    /// The response data structure received by a client after requesting to setup the xServer.
    /// Refer to <see cref="SetupResponse"/>.
    /// </summary>
    public class SetupResponse
    {
        /// <summary>A Base58 cold staking address from the hot or cold wallet accounts.</summary>
        public string Address { get; set; }
    }
}