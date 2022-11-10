using Common.Models.DApps.Models;
using System.Threading.Tasks;

namespace Common.Services
{
    public interface IDappProvisioner
    {
        Task ProvisionNewAppAsync(DappDefinitionModel dappDefinitionModel, DappDeploymentModel deploymentModel);
    }
}