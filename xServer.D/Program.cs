using System;
using System.Threading.Tasks;
using X42.Configuration;
using X42.Feature.Api;
using X42.Feature.Database;
using X42.Feature.X42Client;
using X42.Feature.Network;
using X42.ServerNode;
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
                ServerSettings serverSettings = new ServerSettings(new X42MainServerNode(), ProtocolVersion.PROTOCOL_VERSION, args: args);

                IxServer server = new ServerBuilder()
                    .UseServerSettings(serverSettings)
                    .UseX42Client()
                    .UseSql()
                    .UseApi()
                    .UseNetwork()
                    .Build();

                if (server != null) await server.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"There was a problem initializing xServer. Details: '{ex}'");
            }
        }
    }
}