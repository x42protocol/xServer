namespace x42.Feature.Network
{
    /// <summary>The network startup status.</summary>
    public enum StartupStatus
    {
        NotStarted = 0,
        NodeConnection = 1,
        Database = 2,
        IBD = 3,
        AddressIndexer = 4,
        Profile = 5,
        XServer = 6,
        Started = 100
    }
}
