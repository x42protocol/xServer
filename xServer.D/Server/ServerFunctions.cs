using System.Collections.Generic;
using System.Linq;
using x42.Controllers.Requests;
using x42.Controllers.Results;
using x42.Feature.Database.Context;
using x42.Feature.Database.Tables;

namespace x42.Server
{
    public class ServerFunctions
    {
        private string ConnectionString { get; set; }

        public ServerFunctions(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public TopResult GetTopXServers(int top)
        {
            TopResult result = new TopResult();

            using (X42DbContext dbContext = new X42DbContext(ConnectionString))
            {
                IQueryable<ServerNodeData> servers = dbContext.ServerNodes.Where(n => n.Active).OrderByDescending(u => u.Priority);
                if (top > 0)
                {
                    servers = servers.Take(top);
                }

                if (servers.Count() > 0)
                {
                    servers.ToList().ForEach(
                        x => result.XServers.Add(
                            new XServerConnectionInfo()
                            {
                                Name = x.Name,
                                Address = x.NetworkAddress,
                                Port = x.NetworkPort,
                                Priotiry = x.Priority,
                                Tier = x.Tier
                            }
                    ));
                }
            }

            return result;
        }

        public int GetActiveServerCount()
        {
            int result = 0;

            using (X42DbContext dbContext = new X42DbContext(ConnectionString))
            {
                result = dbContext.ServerNodes.Where(s => s.Active).Count();
            }

            return result;
        }

        public List<RegisterRequest> GetAllActiveXServers()
        {
            List<RegisterRequest> result = new List<RegisterRequest>();

            using (X42DbContext dbContext = new X42DbContext(ConnectionString))
            {
                IQueryable<ServerNodeData> servers = dbContext.ServerNodes.Where(n => n.Active);
                if (servers.Count() > 0)
                {
                    servers.ToList().ForEach(
                        x => result.Add(
                            new RegisterRequest()
                            {
                                Name = x.Name,
                                NetworkAddress = x.NetworkAddress,
                                NetworkPort = x.NetworkPort,
                                Signature = x.Signature,
                                Address = x.PublicAddress,
                                Tier = x.Tier,
                                NetworkProtocol = x.NetworkProtocol
                            }
                    ));
                }
            }

            return result;
        }
    }
}
