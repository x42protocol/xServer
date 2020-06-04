namespace x42.Feature.X42Client.RestClient.Responses
{
    /// <summary>
    /// The response data structure received by a client after requesting a cold staking address.
    /// Refer to <see cref="GetColdStakingAddressRequest"/>.
    /// </summary>
    public class GetColdStakingAddressResponse
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