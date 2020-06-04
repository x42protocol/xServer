namespace x42.Controllers.Results
{
    /// <summary>
    /// The response data structure received by a client after requesting a cold staking address.
    /// Refer to <see cref="SetupResponse"/>.
    /// </summary>
    public class SetupResponse
    {
        /// <summary>A Base58 cold staking address from the hot or cold wallet accounts.</summary>
        public string Address { get; set; }

        /// <summary>Creates a string containing the properties of this object.</summary>
        /// <returns>A string containing the properties of the object.</returns>
        public override string ToString()
        {
            return $"{nameof(this.Address)}={this.Address}";
        }
    }
}