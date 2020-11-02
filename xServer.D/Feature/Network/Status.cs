namespace x42.Feature.Network
{
    /// <summary>The network startup status.</summary>
    public enum StartupStatus
    {
        NotStarted = 0,
        NodeAndDB = 1,
        IBD = 2,
        AddressIndexer = 3,
        NodeConnections = 4,
        Profile = 5,
        XServer = 6,
        Started = 100
    }
}
