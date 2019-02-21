using System.Net;

namespace X42.Utilities
{
    /// <summary>
    ///     Represent a DNS seed.
    ///     This is intended to help servers to connect to the network on their first run.
    ///     As such, DNS seeds must be run by entities in which some level of trust if given by the community running the
    ///     servers.
    /// </summary>
    public class DNSSeedData
    {
        /// <summary> A list of IP addresses associated with this host. </summary>
        private IPAddress[] addresses;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DNSSeedData" /> class.
        /// </summary>
        /// <param name="name">The name given to this DNS seed.</param>
        /// <param name="host">The DNS server host.</param>
        public DNSSeedData(string name, string host)
        {
            Name = name;
            Host = host;
        }

        /// <summary> The name given to this DNS seed. </summary>
        public string Name { get; }

        /// <summary> The DNS server host. </summary>
        public string Host { get; }

        /// <summary>
        ///     Gets the IP addresses of servers associated with the host.
        /// </summary>
        /// <returns>A list of IP addresses.</returns>
        public IPAddress[] GetAddressServers()
        {
            if (addresses != null) return addresses;

            addresses = Dns.GetHostAddressesAsync(Host).GetAwaiter().GetResult();

            return addresses;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Name}({Host})";
        }
    }
}