using Microsoft.Extensions.Primitives;
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
        public decimal Price { get; set; }

        [JsonProperty("usd")]
        public decimal USD { set { Price = value; } }

        [JsonProperty("aed")]
        public decimal AED { set { Price = value; } }

        [JsonProperty("ars")]
        public decimal ARS { set { Price = value; } }

        [JsonProperty("aud")]
        public decimal AUD { set { Price = value; } }

        [JsonProperty("bdt")]
        public decimal BDT { set { Price = value; } }

        [JsonProperty("bhd")]
        public decimal BHD { set { Price = value; } }

        [JsonProperty("bmd")]
        public decimal BMD { set { Price = value; } }

        [JsonProperty("brl")]
        public decimal BRL { set { Price = value; } }

        [JsonProperty("cad")]
        public decimal CAD { set { Price = value; } }

        [JsonProperty("chf")]
        public decimal CHF { set { Price = value; } }

        [JsonProperty("clp")]
        public decimal CLP { set { Price = value; } }

        [JsonProperty("czk")]
        public decimal CZK { set { Price = value; } }

        [JsonProperty("dkk")]
        public decimal DKK { set { Price = value; } }

        [JsonProperty("gbp")]
        public decimal GBP { set { Price = value; } }

        [JsonProperty("hkd")]
        public decimal HKD { set { Price = value; } }

        [JsonProperty("huf")]
        public decimal HUF { set { Price = value; } }

        [JsonProperty("ils")]
        public decimal ILS { set { Price = value; } }

        [JsonProperty("inr")]
        public decimal INR { set { Price = value; } }

        [JsonProperty("kwd")]
        public decimal KWD { set { Price = value; } }

        [JsonProperty("lkr")]
        public decimal LKR { set { Price = value; } }

        [JsonProperty("mmk")]
        public decimal MMK { set { Price = value; } }

        [JsonProperty("mxn")]
        public decimal MXN { set { Price = value; } }

        [JsonProperty("myr")]
        public decimal MYR { set { Price = value; } }

        [JsonProperty("ndk")]
        public decimal NOK { set { Price = value; } }

        [JsonProperty("nzd")]
        public decimal NZD { set { Price = value; } }

        [JsonProperty("php")]
        public decimal PHP { set { Price = value; } }

        [JsonProperty("pkr")]
        public decimal PKR { set { Price = value; } }

        [JsonProperty("pln")]
        public decimal PLN { set { Price = value; } }

        [JsonProperty("sar")]
        public decimal SAR { set { Price = value; } }

        [JsonProperty("sek")]
        public decimal SEK { set { Price = value; } }

        [JsonProperty("sgd")]
        public decimal SGD { set { Price = value; } }

        [JsonProperty("thb")]
        public decimal THB { set { Price = value; } }

        [JsonProperty("try")]
        public decimal TRY { set { Price = value; } }

        [JsonProperty("uah")]
        public decimal UAH { set { Price = value; } }

        [JsonProperty("vef")]
        public decimal VEF { set { Price = value; } }

        [JsonProperty("vnd")]
        public decimal VND { set { Price = value; } }

        [JsonProperty("zar")]
        public decimal ZAR { set { Price = value; } }
    }
}
