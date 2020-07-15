using System.Collections.Generic;
using System.Linq;
using x42.Controllers.Requests;
using x42.Controllers.Results;
using x42.Feature.Database.Context;
using x42.Feature.Database.Tables;

namespace x42.Server
{
    public class ProfileFunctions
    {
        private string ConnectionString { get; set; }

        public ProfileFunctions(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public ProfileData GetProfileByKeyAddress(string keyAddress)
        {
            ProfileData result = null;

            using (X42DbContext dbContext = new X42DbContext(ConnectionString))
            {
                var profile = dbContext.Profiles.Where(p => p.KeyAddress == keyAddress).FirstOrDefault();
                if (profile != null)
                {
                    result = profile;
                }
            }

            return result;
        }

    }
}
