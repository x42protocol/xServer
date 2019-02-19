namespace X42.Protocol
{
    /// <summary>
    /// x42 protocol versioning.
    /// </summary>
    public enum ProtocolVersion : uint
    {
        /// <summary>
        /// Initial protocol version, to be increased after version/verack negotiation.
        /// </summary>
        PROTOCOL_VERSION = 1,
    }
}