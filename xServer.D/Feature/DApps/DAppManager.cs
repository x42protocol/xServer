using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Extensions;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Builders;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using x42.Feature.DApps.Models;
using NBitcoin.DataEncoders;
using System.Diagnostics;

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

        public Task ProvisionNewAppAsync(DappDeploymentModel deploymentModel)
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(),
            (TemplateString)"dapps/wordpress/docker-compose.yml");

            Console.WriteLine("DEBUG: file: "+ file);
            using (var svc = new Builder()
                              .UseContainer()
                              .UseCompose()
                              .FromFile(file)
                              .WithEnvironment(new string[] {"TimeZone=America/New_York",
                                                             "OLS_VERSION=1.7.15",
                                                             "PHP_VERSION=lsphp80",
                                                             "MYSQL_DATABASE=wordpress",
                                                             "MYSQL_ROOT_PASSWORD=password",
                                                             "MYSQL_USER=wordpress",
                                                             "MYSQL_PASSWORD=password",
                                                             "DOMAIN=oti.x42.cloud"}
                              )
                              .RemoveOrphans()
                              .KeepRunning()
                              .Build().Start()) 
            {
                Console.WriteLine("Wordpress State: " + svc.State.ToString());

                ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "./", };
                Process proc = new Process() { StartInfo = startInfo, };
                proc.Start();

            }
            return Task.CompletedTask;


        }
    }


}
