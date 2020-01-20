using System;
using System.Text;
using System.Timers;
using Microsoft.Extensions.Logging;
using X42.Configuration;
using X42.ServerNode;
using X42.Utilities;

namespace X42.Feature.Api
{
    /// <summary>
    ///     Configuration related to the API interface.
    /// </summary>
    public class ApiSettings
    {
        /// <summary>The default host used by the API when the master node runs on the x42 server.</summary>
        public const string DefaultApiHost = "http://0.0.0.0";

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="nodeSettings">The node configuration.</param>
        public ApiSettings(ServerSettings nodeSettings)
        {
            Guard.NotNull(nodeSettings, nameof(nodeSettings));

            logger = nodeSettings.LoggerFactory.CreateLogger(typeof(ApiSettings).FullName);

            TextFileConfiguration config = nodeSettings.ConfigReader;

            UseHttps = config.GetOrDefault("usehttps", false);
            HttpsCertificateFilePath = config.GetOrDefault("certificatefilepath", (string) null);

            if (UseHttps && string.IsNullOrWhiteSpace(HttpsCertificateFilePath))
                throw new ConfigurationException(
                    "The path to a certificate needs to be provided when using https. Please use the argument 'certificatefilepath' to provide it.");

            string defaultApiHost = UseHttps
                ? DefaultApiHost.Replace(@"http://", @"https://")
                : DefaultApiHost;

            string apiHost = config.GetOrDefault("apiuri", defaultApiHost, logger);
            Uri apiUri = new Uri(apiHost);

            // Find out which port should be used for the API.
            int apiPort = config.GetOrDefault("apiport", GetDefaultPort(nodeSettings.ServerNode), logger);

            // If no port is set in the API URI.
            if (apiUri.IsDefaultPort)
            {
                ApiUri = new Uri($"{apiHost}:{apiPort}");
                ApiPort = apiPort;
            }
            // If a port is set in the -apiuri, it takes precedence over the default port or the port passed in -apiport.
            else
            {
                ApiUri = apiUri;
                ApiPort = apiUri.Port;
            }

            // Set the keepalive interval (set in seconds).
            int keepAlive = config.GetOrDefault("keepalive", 0, logger);
            if (keepAlive > 0)
                KeepaliveTimer = new Timer
                {
                    AutoReset = false,
                    Interval = keepAlive * 1000
                };

            // Enable the swagger UI.
            EnableSwagger = config.GetOrDefault("enableswagger", false);
        }

        /// <summary>URI to node's API interface.</summary>
        public Uri ApiUri { get; set; }

        /// <summary>Port of node's API interface.</summary>
        public int ApiPort { get; set; }

        /// <summary>Port of node's API interface.</summary>
        public bool EnableSwagger { get; set; }

        /// <summary>URI to node's API interface.</summary>
        public Timer KeepaliveTimer { get; }

        /// <summary>
        ///     The HTTPS certificate file path.
        /// </summary>
        /// <remarks>
        ///     Password protected certificates are not supported. On MacOs, only p12 certificates can be used without password.
        ///     Please refer to .Net Core documentation for usage:
        ///     <seealso
        ///         cref="https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.-ctor?view=netcore-2.1#System_Security_Cryptography_X509Certificates_X509Certificate2__ctor_System_Byte___" />
        ///     .
        /// </remarks>
        public string HttpsCertificateFilePath { get; set; }

        /// <summary>Use HTTPS or not.</summary>
        public bool UseHttps { get; set; }

        /// <summary>
        ///     Determines the default API port.
        /// </summary>
        /// <param name="network">The network to use.</param>
        /// <returns>The default API port.</returns>
        private static int GetDefaultPort(ServerNodeBase network)
        {
            return network.DefaultPort;
        }

        /// <summary>Prints the help information on how to configure the API settings to the logger.</summary>
        /// <param name="network">The network to use.</param>
        public static void PrintHelp(ServerNodeBase network)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine(
                $"-apiuri=<string>                  URI to node's API interface. Defaults to '{DefaultApiHost}'.");
            builder.AppendLine(
                $"-apiport=<0-65535>                Port of node's API interface. Defaults to {GetDefaultPort(network)}.");
            builder.AppendLine(
                "-keepalive=<seconds>              Keep Alive interval (set in seconds). Default: 0 (no keep alive).");
            builder.AppendLine("-usehttps=<bool>                  Use https protocol on the API. Defaults to false.");
            builder.AppendLine(
                "-certificatefilepath=<string>     Path to the certificate used for https traffic encryption. Defaults to <null>. Password protected files are not supported. On MacOs, only p12 certificates can be used without password.");
            builder.AppendLine("-enableswagger=<bool>             Enable swagger. Defaults to false.");

            ServerSettings.Default(network).Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, ServerNodeBase network)
        {
            builder.AppendLine("####API Settings####");
            builder.AppendLine($"#URI to node's API interface. Defaults to '{DefaultApiHost}'.");
            builder.AppendLine($"#apiuri={DefaultApiHost}");
            builder.AppendLine($"#Port of node's API interface. Defaults to {GetDefaultPort(network)}.");
            builder.AppendLine($"#apiport={GetDefaultPort(network)}");
            builder.AppendLine("#Keep Alive interval (set in seconds). Default: 0 (no keep alive).");
            builder.AppendLine("#keepalive=0");
            builder.AppendLine("#Use HTTPS protocol on the API. Default is false.");
            builder.AppendLine("#usehttps=false");
            builder.AppendLine(
                "#Path to the file containing the certificate to use for https traffic encryption. Password protected files are not supported. On MacOs, only p12 certificates can be used without password.");
            builder.AppendLine(
                @"#Please refer to .Net Core documentation for usage: 'https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.-ctor?view=netcore-2.1#System_Security_Cryptography_X509Certificates_X509Certificate2__ctor_System_Byte___'.");
            builder.AppendLine("#certificatefilepath=");
            builder.AppendLine("#Enable swagger. Defaults to false.");
            builder.AppendLine("#enableswagger=false");
        }
    }
}