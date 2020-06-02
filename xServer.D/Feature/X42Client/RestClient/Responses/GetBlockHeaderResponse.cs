namespace x42.Feature.X42Client.RestClient.Responses
{
    public class GetBlockHeaderResponse
    {
        public int version { get; set; }
        public string merkleroot { get; set; }
        public int nonce { get; set; }
        public string bits { get; set; }
        public string previousblockhash { get; set; }
        public int time { get; set; }
    }
}