using System.Text;
using Microsoft.Extensions.Logging;
using X42.Utilities;
using X42.Configuration;
using X42.MasterNode;

namespace X42.Feature.Database
{
    /// <summary>
    /// Configuration related to the miner interface.
    /// </summary>
    public class DatabaseSettings
    {
        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// An address to use for the database.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="serverSettings">The node configuration.</param>
        public DatabaseSettings(ServerSettings serverSettings)
        {
            Guard.NotNull(serverSettings, nameof(serverSettings));

            this.logger = serverSettings.LoggerFactory.CreateLogger(typeof(DatabaseSettings).FullName);

            TextFileConfiguration config = serverSettings.ConfigReader;

            this.ConnectionString = config.GetOrDefault<string>("connectionstring", "User ID=root;Password=myPassword;Host=localhost;Port=5432;Database=myDataBase;", this.logger);
        }

        /// <summary>
        /// Displays database help information on the console.
        /// </summary>
        /// <param name="masterNode">Not used.</param>
        public static void PrintHelp(MasterNodeBase masterNode)
        {
            ServerSettings defaults = ServerSettings.Default(masterNode);
            var builder = new StringBuilder();

            builder.AppendLine("-connectionstring=<string>                     Database host.");

            defaults.Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, MasterNodeBase network)
        {
            builder.AppendLine("####Database Settings####");
            builder.AppendLine("#Connection string for database.");
            builder.AppendLine("#connectionstring=<string>");
        }
    }
}
