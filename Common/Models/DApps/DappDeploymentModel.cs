using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models.DApps.Models
{
    public class DappDeploymentModel
    {
        public string Domain { get; set; }
        public Dictionary<string,string> Args { get; set; }
       
    }
}
