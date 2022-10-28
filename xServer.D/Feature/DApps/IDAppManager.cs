using System.Threading.Tasks;
using x42.Feature.DApps.Models;

namespace x42.Feature.DApps
{
    public interface IDAppManager
    {
        public Task ProvisionNewAppAsync(DappDeploymentModel deploymentModel);
        Task DeleteAppAsync();
        Task MigrateAppAsync();
        Task FullBackupAppAsync();
    }
}