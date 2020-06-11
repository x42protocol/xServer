using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using x42.Controllers.Requests;
using x42.Feature.Database.Context;
using x42.Feature.Database.Tables;
using x42.Server.Results;

namespace x42.Server
{
    public class SetupServer
    {
        private string ConnectionString { get; set; }

        public enum Status
        {
            NotStarted = 1,
            Started = 2,
            Complete = 3
        }

        public SetupServer(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public bool AddServerToSetup(SetupRequest setupRequest)
        {
            bool result = false;

            using (X42DbContext dbContext = new X42DbContext(ConnectionString))
            {
                IQueryable<ServerData> serverNodes = dbContext.Servers;
                if (serverNodes.Count() == 0)
                {
                    ServerData serverData = new ServerData()
                    {
                        PublicAddress = setupRequest.Address,
                        KeyAddress = setupRequest.KeyAddress,
                        DateAdded = DateTime.UtcNow
                    };

                    var newRecord = dbContext.Add(serverData);
                    if (newRecord.State == EntityState.Added)
                    {
                        dbContext.SaveChanges();
                        result = true;
                    }
                }
            }
            return result;
        }

        public void UpdateServerKey(string KeyAddress)
        {
            using (X42DbContext dbContext = new X42DbContext(ConnectionString))
            {
                ServerData serverNode = dbContext.Servers.FirstOrDefault();
                if (serverNode != null)
                {
                    serverNode.KeyAddress = KeyAddress;

                    dbContext.SaveChanges();
                }
            }
        }

        public SetupStatusResult GetServerSetupStatus()
        {
            SetupStatusResult result = new SetupStatusResult() { ServerStatus = Status.NotStarted };

            using (X42DbContext dbContext = new X42DbContext(ConnectionString))
            {
                IQueryable<ServerData> server = dbContext.Servers;
                if (server.Count() > 0)
                {
                    result.ServerStatus = Status.Started;

                    string keyAddress = server.First().KeyAddress;

                    IQueryable<ServerNodeData> serverNode = dbContext.ServerNodes.Where(s => s.PublicAddress == keyAddress);
                    if (serverNode.Count() > 0)
                    {
                        result.ServerStatus = Status.Complete;
                    }
                }
            }
            return result;
        }

        public string GetServerAddress()
        {
            string result = string.Empty;
            using (X42DbContext dbContext = new X42DbContext(ConnectionString))
            {
                IQueryable<ServerData> server = dbContext.Servers;
                if (server.Count() > 0)
                {
                    result = server.First().PublicAddress;
                }
            }
            return result;
        }
    }
}
