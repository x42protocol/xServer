namespace X42.Feature.X42Client.RestClient.Responses
{
    public class GetWalletHistoryResponse
    {
        public WalletHistory[] history { get; set; }
    }

    public class WalletHistory
    {
        public string accountName { get; set; }
        public string accountHdPath { get; set; }
        public int coinType { get; set; }
        public WalletTransactionshistory[] transactionsHistory { get; set; }
    }

    public class WalletTransactionshistory
    {
        public string type { get; set; }
        public string toAddress { get; set; }
        public string id { get; set; }
        public long amount { get; set; }
        public object[] payments { get; set; }
        public ulong confirmedInBlock { get; set; }
        public string timestamp { get; set; }
    }
}