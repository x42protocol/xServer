using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models.XDocuments.Zones
{
    public class RrSetUpdateModel
    {
        public List<RrSet> Rrsets { get; set; }

        public RrSetUpdateModel(List<RrSet> rrSets)
        {
            Rrsets = rrSets;
        }
    }
}
