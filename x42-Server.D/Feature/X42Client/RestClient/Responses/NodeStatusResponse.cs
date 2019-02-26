namespace X42.Feature.X42Client.RestClient.Responses
{
    public class NodeStatusResponse
    {
        public string agent { get; set; }
        public string version { get; set; }
        public string network { get; set; }
        public string coinTicker { get; set; }
        public int processId { get; set; }
        public ulong consensusHeight { get; set; }
        public ulong blockStoreHeight { get; set; }
        public Outboundpeer[] inboundPeers { get; set; }
        public Outboundpeer[] outboundPeers { get; set; }
        public string[] enabledFeatures { get; set; }
        public string dataDirectoryPath { get; set; }
        public string runningTime { get; set; }
        public float difficulty { get; set; }
        public int protocolVersion { get; set; }
        public bool testnet { get; set; }
        public decimal relayFee { get; set; }
    }
}

public class Outboundpeer
{
    public string version { get; set; }
    public string remoteSocketEndpoint { get; set; }
    public ulong tipHeight { get; set; }
}