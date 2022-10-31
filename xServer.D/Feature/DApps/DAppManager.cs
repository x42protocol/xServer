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
using Ductus.FluentDocker.Model.Builders;
using static System.Net.WebRequestMethods;
using System.Net.Sockets;

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

        public Task ProvisionNewAppAsync(DappDeploymentModel deploymentModel, DappDefinitionModel dappDefinitionModel)
        {
            //dappDefinitionModel.appName = "wordpress";
            //dappDefinitionModel.files = new Models.File[2];

            //dappDefinitionModel.files[0] = new Models.File();
            //dappDefinitionModel.files[0].seq = 0;
            //dappDefinitionModel.files[0].pre = true;
            //dappDefinitionModel.files[0].filename = "pre_deploy_site.sh";
            //dappDefinitionModel.files[0].path = "./";
            //dappDefinitionModel.files[0].content = "IyEvYmluL2Jhc2gNCg0KaWYgWyAkIyAtbHQgNSBdOyB0aGVuDQogIGVjaG8gIlVzYWdlOiAkMCBhcHBuYW1lIGRvbWFpbiBlbWFpbCBteS53cC5kYi5wYXNzd29yZCBteS5yb290LmRiLnBhc3N3b3JkIg0KICBlY2hvICJVc2FnZTogJDAgd29yZHByZXNzIG15d29yZHByZXNzLndvcmRwcmVzc3ByZXZpZXcuc2l0ZSBteUBlbWFpbC5jb20gbXlzZWNldHdwcGFzcyBteXN1cGVyc2VjcmV0cm9vdHBhc3MiDQogIGV4aXQgMQ0KZmkNCg0KQVBQX05BTUU9JDENCkRPTUFJTj0kMg0KRE9NQUlOX0xPV0VSPSQoZWNobyAiJERPTUFJTiIgfCB0ciAnWzp1cHBlcjpdJyAnWzpsb3dlcjpdJyB8IHNlZCAncy9cLi8nXycvZycpDQpFTUFJTD0kMw0KTVlTUUxfUEFTU1dPUkQ9JDQNCk1ZU1FMX1JPT1RfUEFTU1dPUkQ9JDUNCg0KbWFpbigpew0KCWVjaG8gIlNldHRpbmcgVXAgJHtET01BSU5fTE9XRVJ9Ig0KCQ0KCWNkIC9hcHAvZGFwcHMvd29yZHByZXNzDQoNCglta2RpciAtcCBzaXRlcy8ke0RPTUFJTn0NCgkNCglzZWQgLWUgJ3MvI0RPTUFJTiMvJyR7RE9NQUlOfScvZycgLWUgJ3MvI2RvbWFpbiMvJyR7RE9NQUlOX0xPV0VSfScvZycgZG9ja2VyLWNvbXBvc2UueW1sID4gLi9zaXRlcy8ke0RPTUFJTn0vZG9ja2VyLWNvbXBvc2UueW1sDQoJDQoJY3AgLXIgYmluIHNpdGVzLyR7RE9NQUlOfS8NCgljZCBzaXRlcy8ke0RPTUFJTn0NCg0KCW1rZGlyIGFjbWUNCglta2RpciBkYXRhDQoJbWtkaXIgbG9ncw0KCW1rZGlyIGxzd3MNCglta2RpciBzaXRlcw0KDQp9DQoNCm1haW4NCg";

            //dappDefinitionModel.files[1] = new Models.File();
            //dappDefinitionModel.files[1].seq = 1;
            //dappDefinitionModel.files[1].post = true;
            //dappDefinitionModel.files[1].filename = "post_deploy_site.sh";
            //dappDefinitionModel.files[1].path = "./";
            //dappDefinitionModel.files[1].content = "IyEvYmluL2Jhc2gNCg0KaWYgWyAkIyAtbHQgNSBdOyB0aGVuDQogIGVjaG8gIlVzYWdlOiAkMCBhcHBuYW1lIGRvbWFpbiBlbWFpbCBteS53cC5kYi5wYXNzd29yZCBteS5yb290LmRiLnBhc3N3b3JkIg0KICBlY2hvICJVc2FnZTogJDAgd29yZHByZXNzIG15d29yZHByZXNzLndvcmRwcmVzc3ByZXZpZXcuc2l0ZSBteUBlbWFpbC5jb20gbXlzZWNldHdwcGFzcyBteXN1cGVyc2VjcmV0cm9vdHBhc3MiDQogIGV4aXQgMQ0KZmkNCg0KQVBQX05BTUU9JDENCkRPTUFJTj0kMg0KRE9NQUlOX0xPV0VSPSQoZWNobyAiJERPTUFJTiIgfCB0ciAnWzp1cHBlcjpdJyAnWzpsb3dlcjpdJyB8IHNlZCAncy9cLi8nXycvZycpDQpET01BSU5fTk9ET1Q9JChlY2hvICIkRE9NQUlOX0xPV0VSIiB8IHNlZCAncy9cXy8vZycpDQpFTUFJTD0kMw0KTVlTUUxfUEFTU1dPUkQ9JDQNCk1ZU1FMX1JPT1RfUEFTU1dPUkQ9JDUNCg0KbWFpbigpew0KDQoJY2QgL2FwcC9kYXBwcy93b3JkcHJlc3Mvc2l0ZXMvJHtET01BSU59DQoJDQpjYXQgPDxFT0YgPiAuZW52DQpUaW1lWm9uZT1BbWVyaWNhL05ld19Zb3JrDQpPTFNfVkVSU0lPTj0xLjcuMTUNClBIUF9WRVJTSU9OPWxzcGhwODANCk1ZU1FMX0RBVEFCQVNFPXdvcmRwcmVzcw0KTVlTUUxfUk9PVF9QQVNTV09SRD0ke01ZU1FMX1JPT1RfUEFTU1dPUkR9DQpNWVNRTF9VU0VSPXdvcmRwcmVzcw0KTVlTUUxfUEFTU1dPUkQ9JHtNWVNRTF9QQVNTV09SRH0NCkRPTUFJTj0ke0RPTUFJTn0NCkVPRg0KDQoJDQoJDQoJZG9ja2VyIGNvbnRhaW5lciBjcmVhdGUgLS1uYW1lICB0ZW1wX2NvbnRhaW5lciAtdiAke0RPTUFJTl9OT0RPVH1fY29udGFpbmVyOi91c3IvbG9jYWwvYmluIGJ1c3lib3gNCglkb2NrZXIgY3AgLi9iaW4vY29udGFpbmVyLy4gdGVtcF9jb250YWluZXI6L3Vzci9sb2NhbC9iaW4NCglkb2NrZXIgcm0gdGVtcF9jb250YWluZXINCglzbGVlcCA0DQoNCglkb2NrZXIgcnVuIC0tcm0gLWRpdCAtLW5hbWUgIHRlbXBfY29udGFpbmVyIC12ICR7RE9NQUlOX05PRE9UfV9zaXRlczovdmFyL3d3dy92aG9zdHMvJHtET01BSU59LyBhbHBpbmUgYXNoIC1jICJta2RpciAvdmFyL3d3dy92aG9zdHMvJHtET01BSU59LyR7RE9NQUlOfSINCglzbGVlcCA0DQoNCglkb2NrZXIgcnVuIC0tcm0gLWRpdCAtLW5hbWUgIHRlbXBfY29udGFpbmVyIC12ICR7RE9NQUlOX05PRE9UfV9zaXRlczovdmFyL3d3dy92aG9zdHMvJHtET01BSU59LyBhbHBpbmUgYXNoIC1jICJta2RpciAvdmFyL3d3dy92aG9zdHMvJHtET01BSU59LyR7RE9NQUlOfS9odG1sIg0KCXNsZWVwIDQNCg0KCWRvY2tlciBydW4gLS1ybSAtZGl0IC0tbmFtZSAgdGVtcF9jb250YWluZXIgLXYgJHtET01BSU5fTk9ET1R9X3NpdGVzOi92YXIvd3d3L3Zob3N0cy8ke0RPTUFJTn0vIGFscGluZSBhc2ggLWMgIm1rZGlyIC92YXIvd3d3L3Zob3N0cy8ke0RPTUFJTn0vJHtET01BSU59L2xvZ3MiDQoJc2xlZXAgNA0KDQoJZG9ja2VyIHJ1biAtLXJtIC1kaXQgLS1uYW1lICB0ZW1wX2NvbnRhaW5lciAtdiAke0RPTUFJTl9OT0RPVH1fc2l0ZXM6L3Zhci93d3cvdmhvc3RzLyR7RE9NQUlOfS8gYWxwaW5lIGFzaCAtYyAibWtkaXIgL3Zhci93d3cvdmhvc3RzLyR7RE9NQUlOfS8ke0RPTUFJTn0vY2VydHMiDQoJc2xlZXAgNA0KDQoJY2QgL2FwcC9kYXBwcy93b3JkcHJlc3Mvc2l0ZXMvJHtET01BSU59DQoNCgllY2hvICJBZGRpbmcgRG9tYWluICR7RE9NQUlOfSINCglzb3VyY2UgLi9iaW4vZG9tYWluLnNoIC1BICR7RE9NQUlOfQ0KCQ0KCWVjaG8gIkFkZGluZyBEYXRhYmFzZSINCgliYXNoIC4vYmluL2RhdGFiYXNlLnNoIC1EICR7RE9NQUlOfQ0KDQoJDQoJZG9ja2VyIGNvbnRhaW5lciBjcmVhdGUgLS1uYW1lICB0ZW1wX2NvbnRhaW5lciAtdiAke0RPTUFJTl9OT0RPVH1fc2l0ZXM6L3Zhci93d3cvdmhvc3RzIGJ1c3lib3gNCglkb2NrZXIgY3Agc2l0ZXMvJHtET01BSU59Ly5kYl9wYXNzIHRlbXBfY29udGFpbmVyOi92YXIvd3d3L3Zob3N0cy8ke0RPTUFJTn0vLmRiX3Bhc3MNCglkb2NrZXIgcm0gdGVtcF9jb250YWluZXINCg0KCWVjaG8gIkluc3RhbGxpbmcgJHtBUFBfTkFNRX0gb24gJHtET01BSU59Ig0KCWJhc2ggLi9iaW4vYXBwaW5zdGFsbC5zaCAtQSAke0FQUF9OQU1FfSAtRCAke0RPTUFJTn0NCgkNCgllY2hvICJEb25lLiINCn0NCg0KbWFpbg0K";

            //dappDefinitionModel.files[2] = new Models.File();
            //dappDefinitionModel.files[2].dockercompose = true;
            //dappDefinitionModel.files[2].filename = "docker-compose.yml";
            //dappDefinitionModel.files[2].path = "./";
            //dappDefinitionModel.files[2].content = "dmVyc2lvbjogJzMnDQpzZXJ2aWNlczoNCiAgbXlzcWw6DQogICAgaW1hZ2U6IG1hcmlhZGI6MTAuNS45DQogICAgY29tbWFuZDogLS1tYXhfYWxsb3dlZF9wYWNrZXQ9MjU2TQ0KICAgIHZvbHVtZXM6DQogICAgICAtICJkYXRhOi92YXIvbGliL215c3FsIg0KICAgIGVudmlyb25tZW50Og0KICAgICAgTVlTUUxfUk9PVF9QQVNTV09SRDogJHtNWVNRTF9ST09UX1BBU1NXT1JEfQ0KICAgICAgTVlTUUxfREFUQUJBU0U6ICR7TVlTUUxfREFUQUJBU0V9DQogICAgICBNWVNRTF9VU0VSOiAke01ZU1FMX1VTRVJ9DQogICAgICBNWVNRTF9QQVNTV09SRDogJHtNWVNRTF9QQVNTV09SRH0NCiAgICByZXN0YXJ0OiBhbHdheXMNCiAgICBuZXR3b3JrczoNCiAgICAgICNkb21haW4jOg0KICAgICAgICBhbGlhc2VzOg0KICAgICAgICAgIC0gI2RvbWFpbiMNCiAgICBoZWFsdGhjaGVjazoNCiAgICAgIHRlc3Q6IFsiQ01EIiwgIm15c3FsYWRtaW4iICwicGluZyIsICItaCIsICJsb2NhbGhvc3QiXQ0KICAgICAgdGltZW91dDogMjBzDQogICAgICByZXRyaWVzOiA1DQoNCiAgbGl0ZXNwZWVkOg0KICAgIGltYWdlOiBsaXRlc3BlZWR0ZWNoL29wZW5saXRlc3BlZWQ6JHtPTFNfVkVSU0lPTn0tJHtQSFBfVkVSU0lPTn0NCiAgICBlbnZpcm9ubWVudDoNCiAgICAgIFRaOiBBbWVyaWNhL05ld19Zb3JrDQogICAgbGFiZWxzOg0KICAgICAgLSAidHJhZWZpay5lbmFibGU9dHJ1ZSINCiAgICAgIC0gInRyYWVmaWsuZG9ja2VyLm5ldHdvcms9cHJveHkiDQogICAgICAtICJ0cmFlZmlrLmh0dHAucm91dGVycy4jZG9tYWluIy5ydWxlPUhvc3QoYCNET01BSU4jYCkiDQogICAgICAtICJ0cmFlZmlrLmh0dHAucm91dGVycy4jZG9tYWluIy5lbnRyeXBvaW50cz13ZWJzZWN1cmUiDQogICAgICAtICJ0cmFlZmlrLmh0dHAuc2VydmljZXMuI2RvbWFpbiMubG9hZGJhbGFuY2VyLnNlcnZlci5wb3J0PTgwIg0KICAgICAgLSAidHJhZWZpay5odHRwLnJvdXRlcnMuI2RvbWFpbiMudGxzLmNlcnRyZXNvbHZlcj1teXJlc29sdmVyIg0KICAgIHZvbHVtZXM6DQogICAgICAgIC0gbHN3c19jb25mOi91c3IvbG9jYWwvbHN3cy9jb25mDQogICAgICAgIC0gbHN3c19hZG1pbi1jb25mOi91c3IvbG9jYWwvbHN3cy9hZG1pbi9jb25mDQogICAgICAgIC0gY29udGFpbmVyOi91c3IvbG9jYWwvYmluDQogICAgICAgIC0gc2l0ZXM6L3Zhci93d3cvdmhvc3RzLw0KICAgICAgICAtIGFjbWU6L3Jvb3QvLmFjbWUuc2gvDQogICAgICAgIC0gbG9nczovdXNyL2xvY2FsL2xzd3MvbG9ncy8NCiAgICByZXN0YXJ0OiBhbHdheXMNCiAgICBuZXR3b3JrczoNCiAgICAgIHByb3h5Og0KICAgICAgICBhbGlhc2VzOg0KICAgICAgICAgIC0gcHJveHkNCiAgICAgICNkb21haW4jOg0KICAgICAgICBhbGlhc2VzOg0KICAgICAgICAgIC0gI2RvbWFpbiMNCiAgICBkZXBlbmRzX29uOg0KICAgICAgICAgICAgbXlzcWw6DQogICAgICAgICAgICAgICAgY29uZGl0aW9uOiBzZXJ2aWNlX2hlYWx0aHkNCiAgICAgIA0KbmV0d29ya3M6DQogIHByb3h5Og0KICAgIGV4dGVybmFsOiB0cnVlDQogICAgbmFtZTogcHJveHkNCiAgI2RvbWFpbiM6DQogICAgZXh0ZXJuYWw6IGZhbHNlDQogICAgbmFtZTogI2RvbWFpbiMNCnZvbHVtZXM6DQogIGxzd3NfY29uZjoNCiAgbHN3c19hZG1pbi1jb25mOg0KICBjb250YWluZXI6DQogIHNpdGVzOg0KICBhY21lOg0KICBsb2dzOg0KICBkYXRhOg";


            var file = Path.Combine(Directory.GetCurrentDirectory(),
            (TemplateString)"dapps/wordpress/sites/oti.x42.com/docker-compose.yml");

            Console.WriteLine("DEBUG: file: "+ file);


            ProcessStartInfo preInfo = new ProcessStartInfo()
            {
                FileName = "/bin/bash",
                Arguments = "./dapps/wordpress/pre_deploy_site.sh wordpress oti.x42.com psavva@gmail.com password password"
            };

            Process preProc = new Process() { StartInfo = preInfo};
            preProc.Start();


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
                                                             "DOMAIN=oti.x42.com"}
                                )
                              .RemoveOrphans()
                              .KeepRunning()
                            .Build().Start()) 
            {
                Console.WriteLine("Wordpress State: " + svc.State.ToString());


                ProcessStartInfo postInfo = new ProcessStartInfo()
                {
                    FileName = "/bin/bash",
                    Arguments = "./dapps/wordpress/post_deploy_site.sh wordpress oti.x42.com psavva@gmail.com password password"
                };

                Process postProc = new Process() { StartInfo = postInfo};
                postProc.Start();

            }
            return Task.CompletedTask;


        }

        private void GenerateDAppFolders(DappDefinitionModel dappDefinitionModel)
        {
            foreach (var folder in dappDefinitionModel.files.Distinct())
            {
                Directory.CreateDirectory($"dapps\\{dappDefinitionModel.appName}\\{folder.path}");
            };
        }

    }


}
