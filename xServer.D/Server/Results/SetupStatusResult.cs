using x42.ServerNode;
using static x42.Server.SetupServer;

namespace x42.Server.Results
{
    public class SetupStatusResult
    {
        public Status ServerStatus { get; set; }
        public Tier.TierLevel TierLevel { get; set; } = Tier.TierLevel.Seed;
    }
}