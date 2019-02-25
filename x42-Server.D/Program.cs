using System;
using System.Threading.Tasks;
using X42.Configuration;
using X42.Feature.Api;
using X42.Feature.Database;
using X42.Feature.FullNode;
using X42.MasterNode;
using X42.Protocol;
using X42.Server;
using X42.Utilities.Extensions;

namespace X42
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                ServerSettings serverSettings =
                    new ServerSettings(new X42MasterNode(), ProtocolVersion.PROTOCOL_VERSION, args: args);

                IX42Server server = new ServerBuilder()
                    .UseServerSettings(serverSettings)
                    .UseFullNode()
                    .UsePostgreSql()
                    .UseApi()
                    .Build();

                if (server != null) await server.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was a problem initializing x42-Server. Details: '{ex}'");
            }
        }
    }
}