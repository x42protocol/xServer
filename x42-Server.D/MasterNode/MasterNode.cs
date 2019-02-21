using System;
using System.Collections.Generic;
using NBitcoin;
using NBitcoin.Protocol;
using DNSSeedData = X42.Utilities.DNSSeedData;

namespace X42.MasterNode
{
    public abstract class MasterNodeBase
    {
        /// <summary>
        ///     The default port on which servers of this masternode communicate with external clients.
        /// </summary>
        public int DefaultPort { get; protected set; }

        /// <summary>
        ///     The name of the masternode.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        ///     The default name used for the masternode configuration file.
        /// </summary>
        public string DefaultConfigFilename { get; protected set; }

        /// <summary>
        ///     The list of tiers available for the masternode.
        /// </summary>
        public List<Tier> Tiers { get; protected set; }

        /// <summary>
        ///     The list of servers on the masternode that our current server tries to connect to.
        /// </summary>
        public List<NetworkAddress> SeedServers { get; protected set; }

        /// <summary>
        ///     The list of DNS seeds from which to get IP addresses when bootstrapping a server.
        /// </summary>
        public List<DNSSeedData> DNSSeeds { get; protected set; }

        protected IEnumerable<NetworkAddress> ConvertToNetworkAddresses(string[] seeds, int defaultPort)
        {
            var rand = new Random();
            var oneWeek = TimeSpan.FromDays(7);

            foreach (var seed in seeds)
                // It'll only connect to one or two seed servers because once it connects,
                // it'll get a pile of addresses with newer timestamps.
                // Seed servers are given a random 'last seen time' of between one and two weeks ago.
                yield return new NetworkAddress
                {
                    Time = DateTime.UtcNow - TimeSpan.FromSeconds(rand.NextDouble() * oneWeek.TotalSeconds) - oneWeek,
                    Endpoint = Utils.ParseIpEndpoint(seed, defaultPort)
                };
        }
    }
}