using System;
using Microsoft.Extensions.Logging;
using x42.Feature.Database.Context;
using System.Linq;
using x42.Feature.Database.Tables;
using System.Collections.Generic;
using x42.Feature.Database.Repositories;

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

        public Dictionary xServerDictionary;

        private readonly IProfileReservationRepository _profileReservationRepository;

        public DataStore(
            ILoggerFactory loggerFactory,
            DatabaseSettings databaseSettings
,
            IProfileReservationRepository profileReservationRepository)
        {
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.databaseSettings = databaseSettings;
            xServerDictionary = new Dictionary(loggerFactory, databaseSettings);
            _profileReservationRepository = profileReservationRepository;
        }

        public int GetIntFromDictionary(string key)
        {
            return xServerDictionary.Get<int>(key);
        }

        public long GetLongFromDictionary(string key)
        {
            return xServerDictionary.Get<long>(key);
        }

        public string GetStringFromDictionary(string key)
        {
            return xServerDictionary.Get<string>(key);
        }

 

        public bool SetDictionaryValue(string key, object value)
        {
            return xServerDictionary.Set(key, value);
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