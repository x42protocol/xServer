namespace Common.Models.XServer
{
    /// <summary>The network startup status.</summary>
    public enum StartupStatus
    {
        NotStarted = 0,
        NodeConnection = 1,
        Database = 2,
        IBD = 3,
        AddressIndexer = 4,
        XServerConnection = 5,
        Profile = 6,
        XServer = 7,
        Started = 100
    }
}
