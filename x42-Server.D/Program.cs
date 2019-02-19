using System;
using System.Threading.Tasks;
using X42.Server;
using X42.Utilities.Extensions;
using X42.Configuration;
using X42.MasterNode;
using X42.Feature.Api;
using X42.Feature.FullNode;
using X42.Feature.Database;

namespace X42
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var serverSettings = new ServerSettings(new X42MasterNode(), protocolVersion: Protocol.ProtocolVersion.PROTOCOL_VERSION, args: args) { };

                IX42Server server = new ServerBuilder()
                    .UseServerSettings(serverSettings)
                    .UseFullNode()
                    .UsePostgreSQL()
                    .UseApi()
                    .Build();

                if (server != null)
                {
                    await server.RunAsync();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("There was a problem initializing x42-Server. Details: '{0}'", ex.ToString());
            }
        }
    }
}
