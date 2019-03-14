using System.Text;
using Microsoft.Extensions.Logging;
using X42.Configuration;
using X42.MasterNode;
using X42.Utilities;

namespace X42.Feature.Network
{
    /// <summary>
    ///     Configuration related to the network interface.
    /// </summary>
    public class NetworkSettings
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="serverSettings">The node configuration.</param>
        public NetworkSettings(ServerSettings serverSettings)
        {
            Guard.NotNull(serverSettings, nameof(serverSettings));

            logger = serverSettings.LoggerFactory.CreateLogger(typeof(NetworkSettings).FullName);

            TextFileConfiguration config = serverSettings.ConfigReader;
        }

        /// <summary>
        ///     An address to use for the network.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///     Displays network help information on the console.
        /// </summary>
        /// <param name="masterNode">Not used.</param>
        public static void PrintHelp(MasterNodeBase masterNode)
        {
            ServerSettings defaults = ServerSettings.Default(masterNode);
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("-connect=<string>                     masternode host.");

            defaults.Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, MasterNodeBase masterNodeBase)
        {
            builder.AppendLine("####Network Settings####");
            builder.AppendLine("#Manually connect to masternode.");
            builder.AppendLine("#connect=<string>");
        }
    }
}