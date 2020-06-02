namespace x42.Controllers.Results
{
    /// <summary>xServer tier type.</summary>
    public enum TierType
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
}
