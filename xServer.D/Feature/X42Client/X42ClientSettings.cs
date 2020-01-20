using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using X42.Configuration;
using X42.ServerNode;
using X42.Utilities;

namespace X42.Feature.X42Client
{
    /// <summary>
    ///     Configuration related to the database interface.
    /// </summary>
    public class X42ClientSettings
    {
        /// <summary>
        ///     Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="serverSettings">The node configuration.</param>
        public X42ClientSettings(ServerSettings serverSettings)
        {
            Guard.NotNull(serverSettings, nameof(serverSettings));

            ILogger logger = serverSettings.LoggerFactory.CreateLogger(typeof(X42ClientSettings).FullName);

            TextFileConfiguration config = serverSettings.ConfigReader;

            Name = config.GetOrDefault("name", "X42Node", logger);
            Address = config.GetOrDefault("address", IPAddress.Parse("127.0.0.1"), logger);
            Port = config.GetOrDefault("port", (uint)42221, logger);
            SshUserName = config.GetOrDefault("sshusername", "username", logger);
            SshPassword = config.GetOrDefault("sshpassword", "password", logger);
            SshServerAddress = config.GetOrDefault("sshserveraddress", "127.0.0.1", logger);
            SshPort = config.GetOrDefault("sshport", 22, logger);
            SshLocalBoundAddress = config.GetOrDefault("sshlocalboundaddress", "127.0.0.1", logger);
            SshLocalBoundPort = config.GetOrDefault("sshlocalboundport", 42221, logger);
        }

        /// <summary>
        ///     Node name
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Address for X42 Node.
        /// </summary>
        public IPAddress Address { get; }

        /// <summary>
        ///     Port for X42 Node.
        /// </summary>
        public uint Port { get; }

        /// <summary>
        ///     SSH Login user name
        /// </summary>
        public string SshUserName { get; }

        /// <summary>
        ///     SSH Password
        /// </summary>
        public string SshPassword { get; }

        /// <summary>
        ///     Address of SSH Server
        /// </summary>
        public string SshServerAddress { get; }

        /// <summary>
        ///     SSH Server Port
        /// </summary>
        public long SshPort { get; }

        /// <summary>
        ///     IP Address To Bind Locally
        /// </summary>
        public string SshLocalBoundAddress { get; }

        /// <summary>
        ///     Local Port To Bind To
        /// </summary>
        public long SshLocalBoundPort { get; }

        /// <summary>
        ///     Displays database help information on the console.
        /// </summary>
        /// <param name="serverNode">Not used.</param>
        public static void PrintHelp(ServerNodeBase serverNode)
        {
            ServerSettings defaults = ServerSettings.Default(serverNode);
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("-address=127.0.0.1                  Node address.");
            builder.AppendLine("-port=42221                         Node port.");

            defaults.Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, ServerNodeBase network)
        {
            builder.AppendLine("####X42 Node Settings####");
            builder.AppendLine("#Node Address");
            builder.AppendLine("#address=127.0.0.1");
            builder.AppendLine("#Node Port");
            builder.AppendLine("#port=42220");
            builder.AppendLine("");
            builder.AppendLine("####SSH Settings####");
            builder.AppendLine("#SSH Server Address");
            builder.AppendLine("#sshserveraddress=127.0.0.1");
            builder.AppendLine("#SSH Server Port");
            builder.AppendLine("#sshport=22");
            builder.AppendLine("#SSH Server Username");
            builder.AppendLine("#sshusername=username");
            builder.AppendLine("#SSH Password");
            builder.AppendLine("#sshpassword=password");
            builder.AppendLine("#SSH Local Bound Address");
            builder.AppendLine("#sshlocalboundaddress=127.0.0.1");
            builder.AppendLine("#SSH Local Bound Port");
            builder.AppendLine("#sshlocalboundport=42220");
        }
    }
}