using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace x42.Feature.DApps
{
    public class DAppManager : IDAppManager
    {
        private IHostService _docker;
        private readonly ILogger logger;

        public DAppManager(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger(GetType().FullName);


            var hosts = new Hosts().Discover();
            _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");

            var result = _docker.Host.Version(_docker.Certificates);
            logger.LogInformation(result.Data.ToString());
        }
        public DAppManager()
        {

        }

        public Task DeleteAppAsync()
        {
            throw new NotImplementedException();
        }

        public Task FullBackupAppAsync()
        {
            throw new NotImplementedException();
        }

        public Task MigrateAppAsync()
        {
            throw new NotImplementedException();
        }

        public Task ProvisionNewAppAsync(string appName, string[] deployargs)
        {
            // shell.execute (./deploy_script.sh deployargs)

            

            


        }
    }


}
