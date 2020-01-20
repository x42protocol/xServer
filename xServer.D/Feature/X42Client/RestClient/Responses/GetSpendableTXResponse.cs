namespace X42.Feature.X42Client.RestClient.Responses
{
    public class GetSpendableTXResponse
    {
        public GetSpendableTXResponseTransaction[] transactions { get; set; }
    }

    public class GetSpendableTXResponseTransaction
    {
        public string id { get; set; }
        public int index { get; set; }
        public string address { get; set; }
        public bool isChange { get; set; }
        public long amount { get; set; }
        public string creationTime { get; set; }
        public int confirmations { get; set; }
    }
}