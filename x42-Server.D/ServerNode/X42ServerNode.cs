using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using DNSSeedData = X42.Utilities.DNSSeedData;

namespace X42.ServerNode
{
    public class X42ServerNode : ServerNodeBase
    {
        /// <summary> The default name used for the x42 server configuration file. </summary>
        public const string x42DefaultConfigFilename = "x42Server.conf";

        public X42ServerNode()
        {
            Name = "x42 ServerNode";
            DefaultPort = 4242;
            DefaultConfigFilename = x42DefaultConfigFilename;

            List<Tier> Tiers = new List<Tier>
            {
                new Tier(
                    Tier.TierLevel.One,
                    new Collateral {Amount = Money.Coins(1000)}
                ),
                new Tier(
                    Tier.TierLevel.Two,
                    new Collateral {Amount = Money.Coins(20000)}
                ),
                new Tier(
                    Tier.TierLevel.Three,
                    new Collateral {Amount = Money.Coins(100000)}
                )
            };
            this.Tiers = Tiers;

            GracePeriod = 90;

            DNSSeeds = new List<DNSSeedData>
            {
                new DNSSeedData("servernode1.x42seed.host", "servernode1.x42seed.host"),
                new DNSSeedData("servernodeserver1.x42seed.host", "servernodeserver1.x42seed.host"),
                new DNSSeedData("rnode.x42.cloud", "rnode.x42.cloud")
            };

            string[] seedServers = {"63.32.82.169"};
            SeedServers = ConvertToNetworkAddresses(seedServers, DefaultPort).ToList();
        }
    }
}