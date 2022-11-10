using Newtonsoft.Json;

namespace Common.Models.XDocuments.Zones
{
    public class DnsRecord
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("disabled")]
        public bool Disabled { get; set; }

    }
}
