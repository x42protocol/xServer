using Mono.Unix;
using System;
using System.Collections;
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
        public Guid id { get; set; }
        public string appName { get; set; }
        public string description { get; set; }
        public string imageURL { get; set; }
        public int deploymentVersion { get; set; }
        public Dictionary<string, string> envVars { get; set; }
        public DeploymentScriptSet deploymentScriptSet { get; set; }
        public File[] files { get; set; }
    }


    public class JsonForms
    { 
        public object schema { get; set; }
        public object uiSchema { get; set; }
    }

    public class DeploymentScriptSet
    {
        public DeployScript[] deploymentScript { get; set; }
        public JsonForms jsonForms { get; set; }

    }

    public class DeployScript
    {
        public int seq { get; set; }
        public bool preContainer { get; set; } = false;
        public bool postContainer { get; set; } = false;
        public bool composeScript { get; set; } = false;
        public string filename { get; set; }
        public string path { get; set; } = "./";
    }


    public class File
    {
        public string path { get; set; }
        public string filename { get; set; }
        public string content { get; set; }
        public FileAccessPermissions permissions { get; set; } = FileAccessPermissions.GroupReadWriteExecute | FileAccessPermissions.UserReadWriteExecute | FileAccessPermissions.OtherReadWriteExecute;
    }

}
