using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace x42.Feature.DApps.Models
{
    /// <summary>
    /// 
    /// </summary>

    public class DappDefinitionModel
    {
        public string appName { get; set; }
        public string ImageURL { get; set; }
        public int deploymentVersion { get; set; }
        public JsonForms jsonForms { get; set; }
        public DeployScript deployScript { get; set; }
        public File[] files { get; set; }
    }


    public class JsonForms
    {
        public object schema { get; set; }
        public object uiSchema { get; set; }
    }

   

   public class DeployScript
    {
        public string filename { get; set; }
        public Arg[] args { get; set; }
        public string base64content { get; set; }
    }

    public class Arg
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class File
    {
        public string path { get; set; }
        public string filename { get; set; }
        public string content { get; set; }
    }

}
