namespace Common.Models.XServer
{
    public class Tier
    {
        /// <summary>xServer tier type.</summary>
        public enum TierLevel
        {
            /// <summary>Not a tier, but just a seed node.</summary>
            Seed = 0,
            /// <summary>A peer that meets the requirements for a tier 1 node.</summary>
            One = 1,
            /// <summary>A peer that meets the requirements for a tier 2 node.</summary>
            Two = 2,
            /// <summary>A peer that meets the requirements for a tier 3 node.</summary>
            Three = 3
        }

        public Tier(TierLevel level, Collateral collateral)
        {
            Level = level;
            Collateral = collateral;
        }

        public TierLevel Level { get; set; }

        public Collateral Collateral { get; set; }
    }
}