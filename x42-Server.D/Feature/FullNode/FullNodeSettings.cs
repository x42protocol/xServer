using System.Text;
using Microsoft.Extensions.Logging;
using X42.Utilities;
using X42.Configuration;
using X42.MasterNode;

namespace X42.Feature.FullNode
{
    /// <summary>
    /// Configuration related to the miner interface.
    /// </summary>
    public class FullNodeSettings
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// An address to use for the full node.
        /// </summary>
        public string FullNodeHost { get; set; }

        /// <summary>
        /// The fullnode port.
        /// </summary>
        public long FullNodeAPIPort { get; set; }

        /// <summary>
        /// Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="serverSettings">The node configuration.</param>
        public FullNodeSettings(ServerSettings serverSettings)
        {
            Guard.NotNull(serverSettings, nameof(serverSettings));

            this.logger = serverSettings.LoggerFactory.CreateLogger(typeof(FullNodeSettings).FullName);

            TextFileConfiguration config = serverSettings.ConfigReader;

            this.FullNodeHost = config.GetOrDefault<string>("fullnodehost", "localhost", this.logger);

            this.FullNodeAPIPort = config.GetOrDefault<long>("fullnodeapiport", 42220, this.logger);
        }

        /// <summary>
        /// Displays full node help information on the console.
        /// </summary>
        /// <param name="masterNode">Not used.</param>
        public static void PrintHelp(MasterNodeBase masterNode)
        {
            ServerSettings defaults = ServerSettings.Default(masterNode);
            var builder = new StringBuilder();

            builder.AppendLine("-fullnodehost=<string>                     FullNode host.");
            builder.AppendLine("-fullnodeapiport=<string>                  FullNode API port.");

            defaults.Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, MasterNodeBase network)
        {
            builder.AppendLine("####FullNode Settings####");
            builder.AppendLine("#Host for the FullNode.");
            builder.AppendLine("#fullnodehost=<string>");
            builder.AppendLine("#Port for the FullNode API.");
            builder.AppendLine("#fullnodeapiport=<number>");
        }
    }
}
