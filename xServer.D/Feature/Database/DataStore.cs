using System;
using Microsoft.Extensions.Logging;
using x42.Feature.Database.Context;
using System.Linq;
using x42.Feature.Database.Tables;
using System.Collections.Generic;

namespace x42.Feature.Database
{
    /// <inheritdoc />
    /// <summary>
    ///     Holds the data store functionalities.
    /// </summary>
    public class DataStore : IDataStore, IDisposable
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Instance logger.</summary>
        private readonly DatabaseSettings databaseSettings;

        public bool DatabaseConnected { get; set; } = false;

        public DataStore(
            ILoggerFactory loggerFactory,
            DatabaseSettings databaseSettings
            )
        {
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.databaseSettings = databaseSettings;
        }

        public ServerData GetSelfServer()
        {
            ServerData result = null;
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                var selfServer = dbContext.Servers.FirstOrDefault();
                if (selfServer != null)
                {
                    return selfServer;
                }
            }
            return result;
        }

        public int GetProfileReservationCountSearch(string name, string keyAddress)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.ProfileReservations.Where(p => p.Name == name || p.KeyAddress == keyAddress).Count();
            }
        }

        public int GetProfileReservationCountByName(string name)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.ProfileReservations.Where(p => p.Name == name).Count();
            }
        }

        public int GetProfileReservationCountByKeyAddress(string keyAddress)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.ProfileReservations.Where(p => p.KeyAddress == keyAddress).Count();
            }
        }

        public int GetProfileCountSearch(string name, string keyAddress)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.Profiles.Where(p => p.Name == name || p.KeyAddress == keyAddress).Count();
            }
        }

        public int GetProfileCountByName(string name)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.Profiles.Where(p => p.Name == name).Count();
            }
        }

        public int GetProfileCountByKeyAddress(string keyAddress)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.Profiles.Where(p => p.KeyAddress == keyAddress).Count();
            }
        }

        public List<ProfileData> GetFirstProfilesFromBlock(int fromBlock, int take)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.Profiles.Where(p => p.Status == (int)Profile.Status.Created && p.BlockConfirmed > fromBlock).OrderBy(p => p.BlockConfirmed).Take(take).ToList();
            }
        }

        public ProfileData GetProfileByKeyAddress(string keyAddress)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.Profiles.Where(n => n.KeyAddress == keyAddress).FirstOrDefault();
            }
        }

        public ProfileData GetProfileByName(string name)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.Profiles.Where(n => n.Name == name).FirstOrDefault();
            }
        }

        public ProfileReservationData GetProfileReservationByKeyAddress(string keyAddress)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.ProfileReservations.Where(n => n.KeyAddress == keyAddress).FirstOrDefault();
            }
        }

        public ProfileReservationData GetProfileReservationByName(string name)
        {
            using (X42DbContext dbContext = new X42DbContext(databaseSettings.ConnectionString))
            {
                return dbContext.ProfileReservations.Where(n => n.Name == name).FirstOrDefault();
            }
        }

        public void Dispose() { }
    }
}