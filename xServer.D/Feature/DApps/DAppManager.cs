using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Model.Common;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Mono.Unix;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using x42.Feature.DApps.Models;
using x42.Feature.Database;
using static System.Net.Mime.MediaTypeNames;
using File = System.IO.File;

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


        public Task ProvisionNewAppAsync(DappDefinitionModel dappDefinitionModel, DappDeploymentModel deploymentModel)
        {

            var dappFolder = GenerateDAppFolders(dappDefinitionModel);

            foreach (var preScript in dappDefinitionModel.deploymentScriptSet.deploymentScript.Where(_ => _.preContainer == true).OrderBy(_=>_.seq))
            {
                var args = $"{dappFolder}/{SubsituteArgs(preScript.path, deploymentModel.Args)}/{preScript.filename} {ToStringArgs(deploymentModel.Args)}";
                ProcessStartInfo preInfo = new ProcessStartInfo()
                {
                    FileName = "/bin/bash",
                    Arguments = args,
                    WorkingDirectory = dappFolder
                };
                Process preProc = new Process() { StartInfo = preInfo };
                preProc.Start();
            }

            var dockerFileDefinition = dappDefinitionModel.deploymentScriptSet.deploymentScript.Where(_ => _.composeScript == true).FirstOrDefault();

            var composeFile = Path.Combine(Directory.GetCurrentDirectory(),(TemplateString)$"{dappFolder}/{SubsituteArgs(dockerFileDefinition.path, deploymentModel.Args)}/{dockerFileDefinition.filename}"); ;

            Console.WriteLine("DEBUG: file: " + composeFile);

            var EnvironmentVariables = ToENVStringArray(dappDefinitionModel.envVars).Select(s => SubsituteArgs(s, deploymentModel.Args)).ToArray();

            using (var svc = new Builder()
                              .UseContainer()
                              .UseCompose()
                                .FromFile(composeFile)
                                .WithEnvironment(EnvironmentVariables)
                              .RemoveOrphans()
                              .KeepRunning()
                            .Build().Start())
            {
                Console.WriteLine("dApp State: " + svc.State.ToString());

                foreach (var postScript in dappDefinitionModel.deploymentScriptSet.deploymentScript.Where(_ => _.postContainer == true).OrderBy(_ => _.seq))
                {
                    var args = $"{dappFolder}/{SubsituteArgs(postScript.path, deploymentModel.Args)}/{postScript.filename} {ToStringArgs(deploymentModel.Args)}";
                    ProcessStartInfo postInfo = new ProcessStartInfo()
                    {
                        FileName = "/bin/bash",
                        Arguments = args,
                        WorkingDirectory = dappFolder
                    };
                    Process postProc = new Process() { StartInfo = postInfo };
                    postProc.Start();
                }
            }
            return Task.CompletedTask;
        }


        private string ToStringArgs(IDictionary<string, string> dictionary)
        {
            return string.Join(" ", dictionary.Select(kv => kv.Value));
        }

        private string[] ToENVStringArray(IDictionary<string, string> dictionary) {
            return dictionary.Select(item => string.Format("{0}={1}", item.Key.ToUpper(), item.Value))
                    .ToArray();
        }

        private string SubsituteArgs(string path, IDictionary<string, string> dictionary)
        {
            return Regex.Replace(path, @"\{(\w+)\}", m =>
            {
                string value;
                return dictionary.TryGetValue(m.Groups[1].Value, out value) ? value : "null";
            });
        }



        private string GenerateDAppFolders(DappDefinitionModel dappDefinitionModel)
        {
            string guid = Guid.NewGuid().ToString();
            var dappFolder = $"/app/dapps/{dappDefinitionModel.appName}/{guid}";
            try
            {

                //Pre-create the directories
                foreach (var folder in dappDefinitionModel.files.Distinct())
                {
                    Directory.CreateDirectory($"{dappFolder}/{folder.path}");
                };

                //Create Files in their respective paths

                foreach (var file in dappDefinitionModel.files)
                {
                    var path = $"{dappFolder}/{file.path}/{file.filename}";

                    using (FileStream fs = File.Create(path))
                    {
                        byte[] fileContent = Convert.FromBase64String(file.content);
                        // Add some information to the file.
                        fs.Write(fileContent, 0, fileContent.Length);
                    }

                    GrantAccess(path,file.permissions);

                };

            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                throw;
            }

            return dappFolder;

        }

        private void GrantAccess(string fullPath, FileAccessPermissions permissions)
        {
            var unixFileInfo = new UnixFileInfo(fullPath);


            unixFileInfo.FileAccessPermissions = permissions;

        }



    }


}
