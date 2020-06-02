namespace x42.Feature.X42Client.Models
{
    public class BlockHeader
    {
        public int Version { get; set; }
        public string MerkleRoot { get; set; }
        public int Nonce { get; set; }
        public string Bits { get; set; }
        public string PreviousBlockHash { get; set; }
        public string Time { get; set; }
        public ulong Height { get; set; }
    }
}