namespace X42.Feature.X42Client.Enums
{
    public enum ConnectionType
    {
        /// <summary>
        ///     Connected Directly To The API
        /// </summary>
        DirectAPI,

        /// <summary>
        ///     Connected Via SSH & Port Tunneling
        /// </summary>
        SSH,

        /// <summary>
        ///     Connection Is Down
        /// </summary>
        Disconnected
    } //end of public enum ConnectionType
}