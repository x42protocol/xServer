using System;
using System.Linq;
using x42.Controllers.Requests;
using x42.Feature.Database;
using x42.Feature.Database.Context;
using x42.Feature.Database.Tables;
using x42.Server.Results;
using x42.ServerNode;

namespace x42.Server
{
    public class SetupServer
    {
        private string ConnectionString { get; set; }
        private readonly DatabaseFeatures database;

        public enum Status
        {
            NotStarted = 1,
            Started = 2,
            Complete = 3,
            InvalidSignAddress = 4
        }

        public SetupServer(string connectionString, DatabaseFeatures database)
        {
            ConnectionString = connectionString;
            this.database = database;
        }

        public bool AddServerToSetup(SetupRequest setupRequest, string profileName)
        {
            bool result = false;
            var profileNameSet = database.dataStore.SetDictionaryValue("ProfileName", profileName);
            var signAddressSet = database.dataStore.SetDictionaryValue("SignAddress", setupRequest.SignAddress);
            var dateAddedSet = database.dataStore.SetDictionaryValue("DateAdded", DateTime.UtcNow);
            if (profileNameSet && signAddressSet && dateAddedSet)
            {
                result = true;
            }
            return result;
        }

        public void UpdateServerProfileName(string profileName)
        {
            database.dataStore.SetDictionaryValue("ProfileName", profileName);
        }

        public SetupStatusResult GetServerSetupStatus()
        {
            SetupStatusResult result = new SetupStatusResult() { ServerStatus = Status.NotStarted };

            using (X42DbContext dbContext = new X42DbContext(ConnectionString))
            {
                var profileName = database.dataStore.GetStringFromDictionary("ProfileName");
                if (!string.IsNullOrEmpty(profileName))
                {
                    result.ServerStatus = Status.Started;

                    var signAddress = database.dataStore.GetStringFromDictionary("SignAddress");
                    if (!string.IsNullOrEmpty(signAddress))
                    {
                        IQueryable<ServerNodeData> serverNode = dbContext.ServerNodes.Where(s => s.ProfileName == profileName && s.Active);
                        if (serverNode.Count() > 0)
                        {
                            if (serverNode.FirstOrDefault().SignAddress == signAddress)
                            {
                                result.SignAddress = signAddress;
                                result.ServerStatus = Status.Complete;
                                result.TierLevel = (Tier.TierLevel)serverNode.First().Tier;
                            }
                            else
                            {
                                result.ServerStatus = Status.InvalidSignAddress;
                            }
                        }
                    }
                }
            }
            return result;
        }

        public string GetSignAddress()
        {
            return database.dataStore.GetStringFromDictionary("SignAddress");
        }
    }
}
