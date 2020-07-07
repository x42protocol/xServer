using Newtonsoft.Json;

namespace x42.Feature.PriceLock.Results
{
    public partial class CoinGeckoPriceResult
    {
        [JsonProperty("x42-protocol")]
        public X42Protocol X42Protocol { get; set; }
    }

    public partial class X42Protocol
    {
        [JsonProperty("usd")]
        public decimal Usd { get; set; }
    }
}
