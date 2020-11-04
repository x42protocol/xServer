using System.Text;
using Microsoft.Extensions.Logging;
using x42.Configuration;
using x42.ServerNode;
using x42.Utilities;

namespace x42.Feature.Network
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

            BypassTierCheck = config.GetOrDefault("bypasstiercheck", false, logger);
        }

        /// <summary>
        ///     Weather or not to bypass the tier check on startup.
        /// </summary>
        public bool BypassTierCheck { get; set; }

        /// <summary>
        ///     Displays network help information on the console.
        /// </summary>
        /// <param name="serverNode">Not used.</param>
        public static void PrintHelp(ServerNodeBase serverNode)
        {
            ServerSettings defaults = ServerSettings.Default(serverNode);
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("-connect=<string>                     servernode host.");
            builder.AppendLine("-bypasstiercheck=<bool>               Bypass the tier check on startup.");

            defaults.Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, ServerNodeBase serverNodeBase)
        {
            builder.AppendLine("####Network Settings####");
            builder.AppendLine("#Manually connect to servernode.");
            builder.AppendLine("#connect=<string>");
            builder.AppendLine("#bypasstiercheck=<bool>");
        }
    }
}