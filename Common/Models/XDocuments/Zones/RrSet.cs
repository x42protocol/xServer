using Newtonsoft.Json;
using System.Collections.Generic;

namespace Common.Models.XDocuments.Zones
{
    public class RrSet
    {
        [JsonProperty("changetype")]
        public string ChangeType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("ttl")]
        public int Ttl { get; set; }

        [JsonProperty("records")]
        public List<DnsRecord> Records { get; set; }
    }
}
