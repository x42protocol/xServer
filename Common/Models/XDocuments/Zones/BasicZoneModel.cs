using System.Collections.Generic;

namespace Common.Models.XDocuments.Zones
{
    public class BasicZoneModel
    {
        public string Zone { get; set; }
        public List<RrSet> Rrsets { get; set; }
    }
}
