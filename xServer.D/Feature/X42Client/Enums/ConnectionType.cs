namespace x42.Feature.X42Client.Enums
{
    public enum ConnectionType
    {
        /// <summary>
        ///     Connected Directly To The API
        /// </summary>
        DirectApi,

        /// <summary>
        ///     Connected Via SSH & Port Tunneling
        /// </summary>
        Ssh,

        /// <summary>
        ///     Connection Is Down
        /// </summary>
        Disconnected
    } //end of public enum ConnectionType
}