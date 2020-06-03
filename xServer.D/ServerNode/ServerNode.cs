using System;
using System.Collections.Generic;
using NBitcoin;
using NBitcoin.Protocol;
using NodeSeedData = x42.Utilities.NodeSeedData;

namespace x42.ServerNode
{
    public abstract class ServerNodeBase
    {
        /// <summary>
        ///     The default port on which servers of this servernode communicate with external clients.
        /// </summary>
        public int DefaultPort { get; protected set; }

        /// <summary>
        ///     The default port to communicate with the blockchain node over the API.
        /// </summary>
        public int DefaultNodeAPIPort { get; protected set; }

        /// <summary>
        ///     The default port to communicate with the blockchain node.
        /// </summary>
        public int DefaultNodePort { get; protected set; }

        /// <summary>
        ///     The name of the servernode.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        ///     The default name used for the servernode configuration file.
        /// </summary>
        public string DefaultConfigFilename { get; protected set; }

        /// <summary>
        ///     The list of tiers available for the servernode.
        /// </summary>
        public List<Tier> Tiers { get; protected set; }

        /// <summary>
        ///     The list of servers on the servernode that our current server tries to connect to.
        /// </summary>
        public List<NetworkAddress> SeedServers { get; protected set; }

        /// <summary>
        ///     The Grade period (In Minutes) when for how long a node is offline before inactive.
        /// </summary>
        public long DowntimeGracePeriod { get; protected set; }

        /// <summary>
        ///     The Block Grade period (In Block amount) when the node can be behind before inactive.
        /// </summary>
        public ulong BlockGracePeriod { get; protected set; }

        protected IEnumerable<NetworkAddress> ConvertToNetworkAddresses(string[] seeds, int defaultPort)
        {
            Random rand = new Random();
            TimeSpan oneWeek = TimeSpan.FromDays(7);

            foreach (string seed in seeds)
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