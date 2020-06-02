using System.Net;

namespace x42.Configuration
{
    /// <summary>
    ///     Description of network interface on which the server listens.
    /// </summary>
    public class XServerEndpoint
    {
        /// <summary>
        ///     Initializes an instance of the object.
        /// </summary>
        /// <param name="endpoint">IP address and port number on which the server server listens.</param>
        /// <param name="whitelisted">If <c>true</c>, peers that connect to this interface are whitelisted.</param>
        public XServerEndpoint(IPEndPoint endpoint, bool whitelisted)
        {
            Endpoint = endpoint;
            Whitelisted = whitelisted;
        }

        /// <summary>IP address and port number on which the server server listens.</summary>
        public IPEndPoint Endpoint { get; set; }

        /// <summary>If <c>true</c>, peers that connect to this interface are whitelisted.</summary>
        public bool Whitelisted { get; set; }
    }
}