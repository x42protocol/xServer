using NBitcoin;
using System.Collections.Generic;

namespace x42.Feature.X42Client.RestClient.Responses
{
    public class BlockchainInfoResponse
    {
        public string chain { get; set; }
        public uint blocks { get; set; }
        public uint headers { get; set; }
        public string bestblockhash { get; set; }
        public double difficulty { get; set; }
        public long mediantime { get; set; }
        public double verificationprogress { get; set; }
        public bool initialblockdownload { get; set; }
        public string chainwork { get; set; }
        public bool pruned { get; set; }
        public List<Softfork> softforks { get; set; }
        public Dictionary<string, SoftForksBip9> bip9_softforks { get; set; }
    }
    
    public class Softfork
    {
        public string id { get; set; }
        public int version { get; set; }
        public SoftForksStatus reject { get; set; }
    }

    public class SoftForksStatus
    {
        public bool status { get; set; }
    }

    public class SoftForksBip9
    {
        public string status { get; set; }

        public int startTime { get; set; }

        public int timeout { get; set; }

        public int since { get; set; }

        public int bit { get; set; }

        public SoftForksBip9Statistics statistics { get; set; }
    }

    public class SoftForksBip9Statistics
    {
        public int period { get; set; }

        public int threshold { get; set; }

        public int elapsed { get; set; }

        public int count { get; set; }

        public bool possible { get; set; }
    }
}