using System.Collections.Generic;

namespace x42.Feature.PowerDns.Models
{
    public class RRset
    {
        public string Name { get; set; }
        public string ChangeType { get; set; }
        public long Ttl { get; set; }
        public string Type { get; set; }
        public List<RrSetRecord> Records { get; set; }

        public RRset(string name, string changeType, long ttl, string recordType, string content)
        {
            Name = name;
            ChangeType = changeType;
            Ttl = ttl;
            Type = recordType;
            Records = new List<RrSetRecord>() { new RrSetRecord(content, false) };

        }
    }
}
