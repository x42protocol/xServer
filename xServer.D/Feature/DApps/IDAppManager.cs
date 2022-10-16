using System.Threading.Tasks;

namespace x42.Feature.DApps
{
    public interface IDAppManager
    {
        Task ProvisionNewAppAsync();
        Task DeleteAppAsync();
        Task MigrateAppAsync();
        Task FullBackupAppAsync();
    }
}