namespace x42.Feature.X42Client.RestClient.Responses
{
    public class GetBlockResponse
    {
        public BlockTransaction[] transactions { get; set; }
        public string[] tx { get; set; }
        public string hash { get; set; }
        public int size { get; set; }
        public int version { get; set; }
        public string bits { get; set; }
        public string time { get; set; }
        public float difficulty { get; set; }
        public string merkleroot { get; set; }
        public string previousblockhash { get; set; }
        public int nonce { get; set; }
        public ulong height { get; set; }
    }

    public class BlockTransaction
    {
        public string hex { get; set; }
        public string txid { get; set; }
        public int size { get; set; }
        public int version { get; set; }
        public int locktime { get; set; }
        public Vin[] vin { get; set; }
        public Vout[] vout { get; set; }
    }

    public class Vin
    {
        public string coinbase { get; set; }
        public long sequence { get; set; }
        public string txid { get; set; }
        public int vout { get; set; }
        public Scriptsig scriptSig { get; set; }
    }

    public class Scriptsig
    {
        public string asm { get; set; }
        public string hex { get; set; }
    }

    public class Vout
    {
        public float value { get; set; }
        public int n { get; set; }
        public Scriptpubkey scriptPubKey { get; set; }
    }

    public class Scriptpubkey
    {
        public string asm { get; set; }
        public string hex { get; set; }
        public string type { get; set; }
        public int reqSigs { get; set; }
        public string[] addresses { get; set; }
    }
}