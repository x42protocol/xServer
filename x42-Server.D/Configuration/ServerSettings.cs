using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using X42.MasterNode;
using X42.Utilities;
using X42.Configuration.Logging;
using X42.Configuration.Settings;
using X42.Protocol;
using X42.Feature;
using System.Collections.Generic;

namespace X42.Configuration
{
    internal static class NormalizeDirectorySeparatorExt
    {
        /// <summary>
        /// Fixes incorrect directory separator characters in path (if any).
        /// </summary>
        public static string NormalizeDirectorySeparator(this string path)
        {
            // Replace incorrect with correct
            return path.Replace((Path.DirectorySeparatorChar == '/') ? '\\' : '/', Path.DirectorySeparatorChar);
        }
    }

    /// <summary>
    /// Сontains the configuration settings for a x42 server. These settings are taken from both the application
    /// command line arguments and the configuration file.
    /// Unlike the settings held by <see cref="MasterNode"/>, these settings are individualized for each x42 server.
    /// </summary>
    public class ServerSettings : IDisposable
    {
        /// <summary>The version of the protocol supported by the current implementation of the x42 server.</summary>
        public const ProtocolVersion SupportedProtocolVersion = X42.Protocol.ProtocolVersion.PROTOCOL_VERSION;

        /// <summary>A factory responsible for creating a x42 server logger instance.</summary>
        public ILoggerFactory LoggerFactory { get; private set; }

        /// <summary>An instance of the x42 server logger, which reports on the x42 server's activity.</summary>
        public ILogger Logger { get; private set; }

        /// <summary>The settings of the x42 server's logger.</summary>
        public LogSettings Log { get; private set; }

        /// <summary>A list of paths to folders which x42 server components use to store data. These folders are found
        /// in the <see cref="DataDir"/>.
        /// </summary>
        public DataFolder DataFolder { get; private set; }

        /// <summary>The path to the data directory, which contains, for example, the configuration file, wallet files,
        /// and the file containing the peers that the Server has connected to. This value is read-only and can only be
        /// set via the ServerSettings constructor's arguments.
        /// </summary>
        public string DataDir { get; private set; }

        /// <summary>The path to the root data directory, which holds all server data on the machine.
        /// This includes separate subfolders for different servers that run on the machine: a x42 server folder for a
        /// x42 server This value is read-only and can only be set via the ServerSettings constructor's arguments.
        /// </summary>
        public string DataDirRoot { get; private set; }

        /// <summary>The path to the x42 server's configuration file.
        /// This value is read-only and can only be set via the ServerSettings constructor's arguments.
        /// </summary>
        public string ConfigurationFile { get; private set; }

        /// <summary>A combination of the settings from the x42 server's configuration file and the command
        /// line arguments supplied to the x42 server when it was run. This places the settings from both sources
        /// into a single object, which is referenced at runtime.
        /// </summary>
        public TextFileConfiguration ConfigReader { get; private set; }

        /// <summary>The version of the protocol supported by the x42 server.</summary>
        public ProtocolVersion ProtocolVersion { get; private set; }

        /// <summary>The lowest version of the protocol which the x42 server supports.</summary>
        public ProtocolVersion? MinProtocolVersion { get; set; }

        /// <summary>The masternode which the server is configured to run on.</summary>
        public MasterNodeBase MasterNode { get; private set; }

        /// <summary>A string that is used to help identify the x42 server when it connects to other peers on a masternode.
        /// Defaults to "x42Server".
        /// </summary>
        public string Agent { get; private set; }

        /// <summary>
        /// Initializes a new instance of the object.
        /// </summary>
        /// <param name="masterNode">The masternode the server runs on</param>
        /// <param name="protocolVersion">Supported protocol version for which to create the configuration.</param>
        /// <param name="agent">The servers user agent that will be shared with peers.</param>
        /// <param name="args">The command-line arguments.</param>
        /// <exception cref="ConfigurationException">Thrown in case of any problems with the configuration file or command line arguments.</exception>
        /// <remarks>
        /// Processing depends on whether a configuration file is passed via the command line.
        /// There are two main scenarios here:
        /// - The configuration file is passed via the command line. In this case we need
        ///   to read it earlier so that it can provide defaults for "testnet" and "regtest".
        /// - Alternatively, if the file name is not supplied then a masternode-specific file
        ///   name would be determined. In this case we first need to determine the masternode.
        /// </remarks>
        public ServerSettings(MasterNodeBase masterNode, ProtocolVersion protocolVersion = X42.Protocol.ProtocolVersion.PROTOCOL_VERSION, string agent = "x42", string[] args = null)
        {
            this.MasterNode = masterNode;
            // Create the default logger factory and logger.
            var loggerFactory = new ExtendedLoggerFactory();
            this.LoggerFactory = loggerFactory;
            this.LoggerFactory.AddConsoleWithFilters();
            this.LoggerFactory.AddNLog();
            this.Logger = this.LoggerFactory.CreateLogger(typeof(ServerSettings).FullName);
            
            this.ProtocolVersion = protocolVersion;
            this.Agent = agent;
            this.ConfigReader = new TextFileConfiguration(args ?? new string[] { });

            // Log arguments.
            this.Logger.LogDebug("Arguments: masternode='{0}', protocolVersion='{1}', agent='{2}', args='{3}'.",
                this.MasterNode == null ? "(None)" : this.MasterNode.Name,
                this.ProtocolVersion,
                this.Agent,
                args == null ? "(None)" : string.Join(" ", args));

            // By default, we look for a file named '<masternode>.conf' in the masternode's data directory,
            // but both the data directory and the configuration file path may be changed using the -datadir and -conf command-line arguments.
            this.ConfigurationFile = this.ConfigReader.GetOrDefault<string>("conf", null, this.Logger)?.NormalizeDirectorySeparator();
            this.DataDir = this.ConfigReader.GetOrDefault<string>("datadir", null, this.Logger)?.NormalizeDirectorySeparator();
            this.DataDirRoot = this.ConfigReader.GetOrDefault<string>("datadirroot", "x42Server", this.Logger);

            // If the configuration file is relative then assume it is relative to the data folder and combine the paths.
            if (this.DataDir != null && this.ConfigurationFile != null)
            {
                bool isRelativePath = Path.GetFullPath(this.ConfigurationFile).Length > this.ConfigurationFile.Length;
                if (isRelativePath)
                    this.ConfigurationFile = Path.Combine(this.DataDir, this.ConfigurationFile);
            }

            // If the configuration file has been specified on the command line then read it now
            // so that it can provide the defaults for testnet and regtest.
            if (this.ConfigurationFile != null)
            {
                // If the configuration file was specified on the command line then it must exist.
                if (!File.Exists(this.ConfigurationFile))
                    throw new ConfigurationException($"Configuration file does not exist at {this.ConfigurationFile}.");

                // Sets the ConfigReader based on the arguments and the configuration file if it exists.
                this.ReadConfigurationFile();
            }

            // Set the full data directory path.
            if (this.DataDir == null)
            {
                // Create the data directories if they don't exist.
                this.DataDir = this.CreateDefaultDataDirectories(this.MasterNode.Name);
            }
            else
            {
                // Combine the data directory with the masternode's root folder and name.
                string directoryPath = Path.Combine(this.DataDir, this.MasterNode.Name);
                this.DataDir = Directory.CreateDirectory(directoryPath).FullName;
                this.Logger.LogDebug("Data directory initialized with path {0}.", this.DataDir);
            }

            // Set the data folder.
            this.DataFolder = new DataFolder(this.DataDir);

            // Attempt to load NLog configuration from the DataFolder.
            loggerFactory.LoadNLogConfiguration(this.DataFolder);

            // Get the configuration file name for the masternode if it was not specified on the command line.
            if (this.ConfigurationFile == null)
            {
                this.ConfigurationFile = Path.Combine(this.DataDir, this.MasterNode.DefaultConfigFilename);
                this.Logger.LogDebug("Configuration file set to '{0}'.", this.ConfigurationFile);

                if (File.Exists(this.ConfigurationFile))
                    this.ReadConfigurationFile();
            }

            // Create the custom logger factory.
            this.Log = new LogSettings();
            this.Log.Load(this.ConfigReader);
            this.LoggerFactory.AddFilters(this.Log, this.DataFolder);
            this.LoggerFactory.ConfigureConsoleFilters(this.LoggerFactory.GetConsoleSettings(), this.Log);

            // Load the configuration.
            this.LoadConfiguration();
        }

        /// <summary>Determines whether to print help and exit.</summary>
        public bool PrintHelpAndExit
        {
            get
            {
                return this.ConfigReader.GetOrDefault<bool>("help", false, this.Logger) ||
                    this.ConfigReader.GetOrDefault<bool>("-help", false, this.Logger);
            }
        }

        /// <summary>
        /// Initializes default configuration.
        /// </summary>
        /// <param name="masterNode">Specification of the masternode the server runs on - regtest/testnet/mainnet.</param>
        /// <param name="protocolVersion">Supported protocol version for which to create the configuration.</param>
        /// <returns>Default server configuration.</returns>
        public static ServerSettings Default(MasterNodeBase masterNode, ProtocolVersion protocolVersion = SupportedProtocolVersion)
        {
            return new ServerSettings(masterNode, protocolVersion);
        }

        /// <summary>
        /// Creates the configuration file if it does not exist.
        /// </summary>
        /// <param name="features">The features for which to include settings in the configuration file.</param>
        public void CreateDefaultConfigurationFile(List<IFeatureRegistration> features)
        {
            // If the config file does not exist yet then create it now.
            if (!File.Exists(this.ConfigurationFile))
            {
                this.Logger.LogDebug("Creating configuration file '{0}'.", this.ConfigurationFile);

                var builder = new StringBuilder();
                
                File.WriteAllText(this.ConfigurationFile, builder.ToString());
                this.ReadConfigurationFile();
                this.LoadConfiguration();
            }
        }

        /// <summary>
        /// Reads the configuration file and merges it with the command line arguments.
        /// </summary>
        private void ReadConfigurationFile()
        {
            this.Logger.LogDebug("Reading configuration file '{0}'.", this.ConfigurationFile);

            // Add the file configuration to the command-line configuration.
            var fileConfig = new TextFileConfiguration(File.ReadAllText(this.ConfigurationFile));
            fileConfig.MergeInto(this.ConfigReader);
        }

        /// <summary>
        /// Loads the server settings from the application configuration.
        /// </summary>
        private void LoadConfiguration()
        {
            TextFileConfiguration config = this.ConfigReader;
        }

        /// <summary>
        /// Creates default data directories respecting different operating system specifics.
        /// </summary>
        /// <param name="appName">Name of the server, which will be reflected in the name of the data directory.</param>
        /// <param name="masterNode">Specification of the masternode the server runs on - regtest/testnet/mainnet.</param>
        /// <returns>The top-level data directory path.</returns>
        private string CreateDefaultDataDirectories(string appName)
        {
            string directoryPath;

            // Directory paths are different between Windows or Linux/OSX systems.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                {
                    this.Logger.LogDebug("Using HOME environment variable for initializing application data.");
                    directoryPath = Path.Combine(home, "." + appName.ToLowerInvariant());
                }
                else
                {
                    throw new DirectoryNotFoundException("Could not find HOME directory.");
                }
            }
            else
            {
                string localAppData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(localAppData))
                {
                    this.Logger.LogDebug("Using APPDATA environment variable for initializing application data.");
                    directoryPath = Path.Combine(localAppData, appName);
                }
                else
                {
                    throw new DirectoryNotFoundException("Could not find APPDATA directory.");
                }
            }

            // Create the data directories if they don't exist.
            Directory.CreateDirectory(directoryPath);

            this.Logger.LogDebug("Data directory initialized with path {0}.", directoryPath);
            return directoryPath;
        }

        /// <summary>
        /// Displays command-line help.
        /// </summary>
        /// <param name="masterNode">The masternode to extract values from.</param>
        public static void PrintHelp(MasterNodeBase masterNode)
        {
            Guard.NotNull(masterNode, nameof(masterNode));

            ServerSettings defaults = Default(masterNode);
            string daemonName = Path.GetFileName(Assembly.GetEntryAssembly().Location);

            var builder = new StringBuilder();
            builder.AppendLine("Usage:");
            builder.AppendLine($" dotnet run {daemonName} [arguments]");
            builder.AppendLine();
            builder.AppendLine("Command line arguments:");
            builder.AppendLine();
            builder.AppendLine($"-help/--help              Show this help.");
            builder.AppendLine($"-conf=<Path>              Path to the configuration file. Defaults to {defaults.ConfigurationFile}.");
            builder.AppendLine($"-datadir=<Path>           Path to the data directory. Defaults to {defaults.DataDir}.");
            builder.AppendLine($"-debug[=<string>]         Set 'Debug' logging level. Specify what to log via e.g. '-debug=X42.Protocol,X42.Server.Builder'.");
            builder.AppendLine($"-loglevel=<string>        Direct control over the logging level: '-loglevel=trace/debug/info/warn/error/fatal'.");
            
            defaults.Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        /// Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="masterNode">The masternode to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, MasterNodeBase masterNode)
        {
            builder.AppendLine("####Server Settings####");
            builder.AppendLine();
            
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.LoggerFactory.Dispose();
        }
    }
}