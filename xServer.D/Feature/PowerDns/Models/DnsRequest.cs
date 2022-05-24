using System.Collections.Generic;

namespace x42.Feature.PowerDns.Models
{
    public class DnsRequest
    {
        public List<RRset> Rrsets { get; set; }
    }
}
