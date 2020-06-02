namespace x42.Feature.X42Client.Models
{
    public class Peer
    {
        /// <summary>
        ///     IP Address of The Peer
        /// </summary>
        public string Address;

        /// <summary>
        ///     Ban Score of The Node
        /// </summary>
        public int BanScore;

        /// <summary>
        ///     Did The Node Connect To Us
        /// </summary>
        public bool InboundConnection;

        /// <summary>
        ///     Protocol Version of The Node
        /// </summary>
        public int ProtocolVersion;

        /// <summary>
        ///     What Services Are Offered By The Node
        /// </summary>
        public string Services;

        /// <summary>
        ///     Peer Current Block Height
        /// </summary>
        public ulong TipHeight;

        /// <summary>
        ///     Peer Version Information
        /// </summary>
        public string Version;

        /// <summary>
        ///     Will The Node Relay TX's
        /// </summary>
        public bool WillRelayTXs;
    }
}