namespace X42.Feature.X42Client.RestClient.Responses
{
    public class GetPeerInfoResponse
    {
        public int id { get; set; }
        public string addr { get; set; }
        public string addrlocal { get; set; }
        public string services { get; set; }
        public bool relaytxes { get; set; }
        public uint lastsend { get; set; }
        public int lastrecv { get; set; }
        public uint bytessent { get; set; }
        public uint bytesrecv { get; set; }
        public uint conntime { get; set; }
        public uint timeoffset { get; set; }
        public uint pingtime { get; set; }
        public uint minping { get; set; }
        public uint pingwait { get; set; }
        public int version { get; set; }
        public string subver { get; set; }
        public bool inbound { get; set; }
        public bool addnode { get; set; }
        public ulong startingheight { get; set; }
        public int banscore { get; set; }
        public ulong synced_headers { get; set; }
        public ulong synced_blocks { get; set; }
        public bool whitelisted { get; set; }
        public object inflight { get; set; }
        public object bytessent_per_msg { get; set; }
        public object bytesrecv_per_msg { get; set; }
    } //end of GetPeerInfoResponse
}