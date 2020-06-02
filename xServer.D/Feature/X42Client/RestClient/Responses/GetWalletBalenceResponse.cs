namespace x42.Feature.X42Client.RestClient.Responses
{
    public class GetWalletBalenceResponse
    {
        public AccountBalance[] balances { get; set; }
    }

    public class AccountBalance
    {
        public string accountName { get; set; }
        public string accountHdPath { get; set; }
        public int coinType { get; set; }
        public long amountConfirmed { get; set; }
        public long amountUnconfirmed { get; set; }
    }
}