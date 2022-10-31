using System.Threading.Tasks;
using x42.Feature.DApps.Models;

namespace x42.Feature.DApps
{
    public interface IDAppManager
    {
        public Task ProvisionNewAppAsync(DappDeploymentModel deploymentModel, DappDefinitionModel dappDefinitionModel);
        Task DeleteAppAsync();
        Task MigrateAppAsync();
        Task FullBackupAppAsync();
    }
}