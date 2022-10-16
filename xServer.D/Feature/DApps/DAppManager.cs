using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace x42.Feature.DApps
{
    public class DAppManager : IDAppManager
    {
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

        public Task ProvisionNewApp()
        {
            
            return Task.CompletedTask;
        }

        public Task ProvisionNewAppAsync()
        {
            throw new NotImplementedException();
        }
    }


}
