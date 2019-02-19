using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using X42.Utilities;
using X42.Utilities.Extensions;
using X42.MasterNode;

namespace X42.Configuration.Settings
{
    /// <summary>
    /// Configuration related to incoming and outgoing connections.
    /// </summary>
    public sealed class ConnectionManagerSettings
    {
        /// <summary>Number of seconds to keep misbehaving peers from reconnecting (Default 24-hour ban).</summary>
        public const int DefaultMisbehavingBantimeSeconds = 24 * 60 * 60;
        public const int DefaultMisbehavingBantimeSecondsTestnet = 10 * 60;

        /// <summary>Maximum number of AgentPrefix characters to use in the Agent value.</summary>
        private const int MaximumAgentPrefixLength = 10;

        /// <summary>Default value for "blocksonly" option.</summary>
        /// <seealso cref="RelayTxes"/>
        private const bool DefaultBlocksOnly = false;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// Initializes an instance of the object from the server configuration.
        /// </summary>
        /// <param name="serverSettings">The server configuration.</param>
        public ConnectionManagerSettings(ServerSettings serverSettings)
        {
            Guard.NotNull(serverSettings, nameof(serverSettings));

            this.logger = serverSettings.LoggerFactory.CreateLogger(typeof(ConnectionManagerSettings).FullName);
            
            this.AddServer = new List<IPEndPoint>();
            this.Whitelist = new List<IPEndPoint>();

            TextFileConfiguration config = serverSettings.ConfigReader;

            try
            {
                this.Connect.AddRange(config.GetAll("connect", this.logger)
                    .Select(c => c.ToIPEndPoint(serverSettings.MasterNode.DefaultPort)));
            }
            catch (FormatException)
            {
                throw new ConfigurationException("Invalid 'connect' parameter.");
            }

            try
            {
                this.AddServer.AddRange(config.GetAll("addserver", this.logger)
                        .Select(c => c.ToIPEndPoint(serverSettings.MasterNode.DefaultPort)));
            }
            catch (FormatException)
            {
                throw new ConfigurationException("Invalid 'addserver' parameter.");
            }

            this.Port = config.GetOrDefault<int>("port", serverSettings.MasterNode.DefaultPort, this.logger);

            try
            {
                this.Listen.AddRange(config.GetAll("bind").Select(c => new ServerServerEndpoint(c.ToIPEndPoint(this.Port), false)));
            }
            catch (FormatException)
            {
                throw new ConfigurationException("Invalid 'bind' parameter");
            }

            try
            {
                IEnumerable<IPEndPoint> whitebindEndpoints = config.GetAll("whitebind", this.logger).Select(s => s.ToIPEndPoint(this.Port));

                List<IPEndPoint> networkEndpoints = this.Listen.Select(x => x.Endpoint).ToList();

                foreach (IPEndPoint whiteBindEndpoint in whitebindEndpoints)
                {
                    if (whiteBindEndpoint.CanBeMappedTo(networkEndpoints, out IPEndPoint outEndpoint))
                    {
                        // White-list white-bind endpoint if we are currently listening to it.
                        ServerServerEndpoint listenToThisEndpoint = this.Listen.SingleOrDefault(x => x.Endpoint.Equals(outEndpoint));

                        if (listenToThisEndpoint != null)
                            listenToThisEndpoint.Whitelisted = true;
                    }
                    else
                    {
                        // Add to list of network interfaces if we are not.
                        this.Listen.Add(new ServerServerEndpoint(whiteBindEndpoint, true));
                    }
                }
            }
            catch (FormatException)
            {
                throw new ConfigurationException("Invalid 'whitebind' parameter");
            }

            if (this.Listen.Count == 0)
            {
                this.Listen.Add(new ServerServerEndpoint(new IPEndPoint(IPAddress.Parse("0.0.0.0"), this.Port), false));
            }
            else
            {
                var ports = this.Listen.Select(l => l.Endpoint.Port).ToList();

                if (ports.Count != ports.Distinct().Count())
                {
                    throw new ConfigurationException("Invalid attempt to bind the same port twice");
                }
            }

            try
            {
                this.Whitelist.AddRange(config.GetAll("whitelist", this.logger)
                    .Select(c => c.ToIPEndPoint(serverSettings.MasterNode.DefaultPort)));
            }
            catch (FormatException)
            {
                throw new ConfigurationException("Invalid 'whitelist' parameter.");
            }

            string externalIp = config.GetOrDefault<string>("externalip", null, this.logger);
            if (externalIp != null)
            {
                try
                {
                    this.ExternalEndpoint = externalIp.ToIPEndPoint(this.Port);
                }
                catch (FormatException)
                {
                    throw new ConfigurationException("Invalid 'externalip' parameter");
                }
            }

            if (this.ExternalEndpoint == null)
            {
                this.ExternalEndpoint = new IPEndPoint(IPAddress.Loopback, this.Port);
            }

            this.BanTimeSeconds = config.GetOrDefault<int>("bantime", DefaultMisbehavingBantimeSeconds, this.logger);
            this.InitialConnectionTarget = config.GetOrDefault("initialconnectiontarget", 1, this.logger);
            this.SyncTimeEnabled = config.GetOrDefault<bool>("synctime", true, this.logger);
            this.RelayTxes = !config.GetOrDefault("blocksonly", DefaultBlocksOnly, this.logger);
            this.IpRangeFiltering = config.GetOrDefault<bool>("IpRangeFiltering", true, this.logger);

            var agentPrefix = config.GetOrDefault("agentprefix", string.Empty, this.logger).Replace("-", string.Empty);
            if (agentPrefix.Length > MaximumAgentPrefixLength)
                agentPrefix = agentPrefix.Substring(0, MaximumAgentPrefixLength);

            this.Agent = string.IsNullOrEmpty(agentPrefix) ? serverSettings.Agent : $"{agentPrefix}-{serverSettings.Agent}";
            this.logger.LogDebug("Agent set to '{0}'.", this.Agent);
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="masterNode">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, MasterNodeBase masterNode)
        {
            builder.AppendLine("####ConnectionManager Settings####");
            builder.AppendLine($"#The default network port to connect to. Default { masterNode.DefaultPort }.");
            builder.AppendLine($"#port={masterNode.DefaultPort}");
            builder.AppendLine($"#Add a server to connect to and attempt to keep the connection open. Can be specified multiple times.");
            builder.AppendLine($"#addserver=<ip:port>");
            builder.AppendLine($"#Bind to given address and whitelist peers connecting to it. Use [host]:port notation for IPv6. Can be specified multiple times.");
            builder.AppendLine($"#whitebind=<ip:port>");
            builder.AppendLine($"#Whitelist peers having the given IP:port address, both inbound or outbound. Can be specified multiple times.");
            builder.AppendLine($"#whitelist=<ip:port>");
            builder.AppendLine($"#Specify your own public address.");
            builder.AppendLine($"#externalip=<ip>");
            builder.AppendLine($"#Number of seconds to keep misbehaving peers from reconnecting. Default {ConnectionManagerSettings.DefaultMisbehavingBantimeSeconds}.");
            builder.AppendLine($"#bantime=<number>");
            builder.AppendLine($"#Sync with peers. Default 1.");
            builder.AppendLine($"#synctime=1");
            builder.AppendLine($"#An optional prefix for the server's user agent shared with peers. Truncated if over { MaximumAgentPrefixLength } characters.");
            builder.AppendLine($"#agentprefix=<string>");
            builder.AppendLine($"#Enable bandwidth saving setting to send and received confirmed blocks only. Defaults to { (DefaultBlocksOnly ? 1 : 0) }.");
            builder.AppendLine($"#blocksonly={ (DefaultBlocksOnly ? 1 : 0) }");
            builder.AppendLine($"#bantime=<number>");
            builder.AppendLine($"#Disallow connection to peers in same IP range. Default is 1 for remote hosts.");
            builder.AppendLine($"#iprangefiltering=<0 or 1>");
        }

        /// <summary>
        /// Displays command-line help.
        /// </summary>
        /// <param name="masterNode">The network to extract values from.</param>
        public static void PrintHelp(MasterNodeBase masterNode)
        {
            Guard.NotNull(masterNode, nameof(masterNode));

            var defaults = ServerSettings.Default(masterNode);

            var builder = new StringBuilder();
            builder.AppendLine($"-port=<port>              The default network port to connect to. Default { masterNode.DefaultPort }.");
            builder.AppendLine($"-connect=<ip:port>        Specified server to connect to. Can be specified multiple times.");
            builder.AppendLine($"-addserver=<ip:port>        Add a server to connect to and attempt to keep the connection open. Can be specified multiple times.");
            builder.AppendLine($"-whitebind=<ip:port>      Bind to given address and whitelist peers connecting to it. Use [host]:port notation for IPv6. Can be specified multiple times.");
            builder.AppendLine($"-whitelist=<ip:port>      Whitelist peers having the given IP:port address, both inbound or outbound. Can be specified multiple times.");
            builder.AppendLine($"-externalip=<ip>          Specify your own public address.");
            builder.AppendLine($"-bantime=<number>         Number of seconds to keep misbehaving peers from reconnecting. Default {ConnectionManagerSettings.DefaultMisbehavingBantimeSeconds}.");
            builder.AppendLine($"-synctime=<0 or 1>        Sync with peers. Default 1.");
            builder.AppendLine($"-agentprefix=<string>     An optional prefix for the server's user agent that will be shared with peers in the version handshake.");
            builder.AppendLine($"-blocksonly=<0 or 1>      Enable bandwidth saving setting to send and received confirmed blocks only. Defaults to { DefaultBlocksOnly }.");
            builder.AppendLine($"-iprangefiltering=<0 or 1> Disallow connection to peers in same IP range. Default is 1 for remote hosts.");

            defaults.Logger.LogInformation(builder.ToString());
        }

        /// <summary>List of exclusive end points that the server should be connected to.</summary>
        public List<IPEndPoint> Connect { get; set; }

        /// <summary>List of end points that the server should try to connect to.</summary>
        public List<IPEndPoint> AddServer { get; set; }

        /// <summary>List of network interfaces on which the server should listen on.</summary>
        public List<ServerServerEndpoint> Listen { get; set; }

        /// <summary>External (or public) IP address of the server.</summary>
        public IPEndPoint ExternalEndpoint { get; internal set; }

        /// <summary>Port of the server.</summary>
        public int Port { get; internal set; }

        /// <summary>Number of seconds to keep misbehaving peers from reconnecting.</summary>
        public int BanTimeSeconds { get; internal set; }

        /// <summary>Maximum number of outbound connections.</summary>
        public int MaxOutboundConnections { get; internal set; }

        /// <summary>Maximum number of inbound connections.</summary>
        public int MaxInboundConnections { get; internal set; }

        /// <summary>
        /// The amount of connections to be reached before a 1 second connection interval in the <see cref="P2P.PeerConnectorDiscovery"/> is set.
        /// <para>
        /// When the <see cref="P2P.PeerConnectorDiscovery"/> starts up, a 100ms delay is set as the connection interval in order for
        /// the server to quickly connect to other peers.
        /// </para>
        /// </summary>
        public int InitialConnectionTarget { get; internal set; }

        /// <summary><c>true</c> to sync time with other peers and calculate adjusted time, <c>false</c> to use our system clock only.</summary>
        public bool SyncTimeEnabled { get; private set; }

        /// <summary>The server's user agent.</summary>
        public string Agent { get; private set; }

        /// <summary><c>true</c> to enable bandwidth saving setting to send and received confirmed blocks only.</summary>
        public bool RelayTxes { get; set; }

        /// <summary>Filter peers that are within the same IP range to prevent sybil attacks.</summary>
        public bool IpRangeFiltering { get; internal set; }

        /// <summary>List of white listed IP endpoint. The server will flags peers that connects to the server, or that the server connects to, as whitelisted.</summary>
        public List<IPEndPoint> Whitelist { get; set; }
    }
}