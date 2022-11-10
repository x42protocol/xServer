using Common.Models.DApps.Models;
using System.Threading.Tasks;

namespace x42.Feature.DApps
{
    public interface IDAppManager
    {
        public Task ProvisionNewAppAsync(DappDefinitionModel dappDefinitionModel, DappDeploymentModel deploymentModel);
        Task DeleteAppAsync();
        Task MigrateAppAsync();
        Task FullBackupAppAsync();
    }
}