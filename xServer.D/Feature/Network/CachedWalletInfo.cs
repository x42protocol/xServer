namespace x42.Feature.Network
{
    /// <summary>
    ///     Provides an ability to sign requests
    /// </summary>
    public class CachedWalletInfo
    {
        public string WalletName { get; set; }

        public string Password { get; set; }

        public string AccountName { get; set; }

        public string SignAddress { get; set; }
    }
}
