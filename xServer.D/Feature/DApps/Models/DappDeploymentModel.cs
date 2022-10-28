using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace x42.Feature.DApps.Models
{
    public class DappDeploymentModel
    {
        public string Name { get; set; }
        public Dictionary<string,string>[] Args { get; set; }
        public string DeploymentManifest { get; set; }

    }
}
