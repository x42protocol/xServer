namespace X42.MasterNode
{
    public class Tier
    {
        public enum TierLevel
        {
            One = 1,
            Two = 2,
            Three = 3
        }

        public TierLevel Level { get; set; }

        public Collateral Collateral { get; set; }

        public Tier(TierLevel level, Collateral collateral)
        {
            Level = level;
            Collateral = collateral;
        }

    }
}
