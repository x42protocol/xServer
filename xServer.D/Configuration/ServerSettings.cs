using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using x42.Configuration.Logging;
using x42.Configuration.Settings;
using x42.Feature.Setup;
using x42.ServerNode;
using x42.Protocol;
using x42.Utilities;

namespace x42.Configuration
{
    internal static class NormalizeDirectorySeparatorExt
    {
        /// <summary>
        ///     Fixes incorrect directory separator characters in path (if any).
        /// </summary>
        public static string NormalizeDirectorySeparator(this string path)
        {
            // Replace incorrect with correct
            return path.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
        }
    }

    /// <summary>
    ///     Сontains the configuration settings for a xServer. These settings are taken from both the application
    ///     command line arguments and the configuration file.
    ///     Unlike the settings held by <see cref="x42.ServerNode" />, these settings are individualized for each xServer.
    /// </summary>
    public class ServerSettings : IDisposable
    {
        /// <summary>The version of the protocol supported by the current implementation of the xServer.</summary>
        public const ProtocolVersion SupportedProtocolVersion = ProtocolVersion.PROTOCOL_VERSION;

        public string ServerName = "xServer";

        /// <summary>
        ///     Initializes a new instance of the object.
        /// </summary>
        /// <param name="serverNode">The servernode the server runs on</param>
        /// <param name="protocolVersion">Supported protocol version for which to create the configuration.</param>
        /// <param name="agent">The servers user agent that will be shared with peers.</param>
        /// <param name="args">The command-line arguments.</param>
        /// <exception cref="ConfigurationException">
        ///     Thrown in case of any problems with the configuration file or command line
        ///     arguments.
        /// </exception>
        /// <remarks>
        ///     Processing depends on whether a configuration file is passed via the command line.
        ///     There are two main scenarios here:
        ///     - The configuration file is passed via the command line. In this case we need
        ///     to read it earlier so that it can provide defaults for "testnet" and "regtest".
        ///     - Alternatively, if the file name is not supplied then a servernode-specific file
        ///     name would be determined. In this case we first need to determine the servernode.
        /// </remarks>
        public ServerSettings(ServerNodeBase serverNode, ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION, string agent = "x42", string[] args = null)
        {
            // Create the default logger factory and logger.
            LoggerFactory = ExtendedLoggerFactory.Create(Log);
            Logger = LoggerFactory.CreateLogger(typeof(ServerSettings).FullName);

            ProtocolVersion = protocolVersion;
            Agent = agent;
            ConfigReader = new TextFileConfiguration(args ?? new string[] { });

            // Log arguments.
            Logger.LogDebug("Arguments: servernode='{0}', protocolVersion='{1}', agent='{2}', args='{3}'.",
                string.IsNullOrEmpty(ServerName) ? "(None)" : ServerName,
                ProtocolVersion,
                Agent,
                args == null ? "(None)" : string.Join(" ", args));

            // By default, we look for a file named '<servernode>.conf' in the servernode's data directory,
            // but both the data directory and the configuration file path may be changed using the -datadir and -conf command-line arguments.
            ConfigurationFile = ConfigReader.GetOrDefault<string>("conf", null, Logger)?.NormalizeDirectorySeparator();
            DataDir = ConfigReader.GetOrDefault<string>("datadir", null, Logger)?.NormalizeDirectorySeparator();
            DataDirRoot = ConfigReader.GetOrDefault("datadirroot", "xServerData", Logger);

            // If the configuration file is relative then assume it is relative to the data folder and combine the paths.
            if (DataDir != null && ConfigurationFile != null)
            {
                bool isRelativePath = Path.GetFullPath(ConfigurationFile).Length > ConfigurationFile.Length;
                if (isRelativePath)
                    ConfigurationFile = Path.Combine(DataDir, ConfigurationFile);
            }

            // If the configuration file has been specified on the command line then read it now
            // so that it can provide the defaults for testnet and regtest.
            if (ConfigurationFile != null)
            {
                // If the configuration file was specified on the command line then it must exist.
                if (!File.Exists(ConfigurationFile))
                    throw new ConfigurationException($"Configuration file does not exist at {ConfigurationFile}.");

                // Sets the ConfigReader based on the arguments and the configuration file if it exists.
                ReadConfigurationFile();
            }

            // Find out if we need to run on testnet from the config file.
            bool testNet = ConfigReader.GetOrDefault("testnet", false, Logger);
            if (testNet)
            {
                ServerNode = new X42TestServerNode();
            }
            else
            {
                ServerNode = serverNode;
            }


            // Set the full data directory path.
            if (DataDir == null)
            {
                // Create the data directories if they don't exist.
                DataDir = CreateDefaultDataDirectories(Path.Combine($"{ServerName}Data", testNet ? "Test" : "Main"));
            }
            else
            {
                // Combine the data directory with the servernode's root folder and name.
                string directoryPath = Path.Combine(DataDir, ServerName);
                DataDir = Directory.CreateDirectory(directoryPath).FullName;
                Logger.LogDebug("Data directory initialized with path {0}.", DataDir);
            }

            // Set the data folder.
            DataFolder = new DataFolder(DataDir);

            // Get the configuration file name for the servernode if it was not specified on the command line.
            if (ConfigurationFile == null)
            {
                ConfigurationFile = Path.Combine(DataDir, ServerNode.DefaultConfigFilename);
                Logger.LogDebug("Configuration file set to '{0}'.", ConfigurationFile);

                if (File.Exists(ConfigurationFile))
                    ReadConfigurationFile();
            }

            // Create the custom logger factory.
            Log = new LogSettings();
            Log.Load(ConfigReader);
            LoggerFactory.AddNLog();
            LoggerFactory.LoadNLogConfiguration(DataFolder);
            LoggerFactory.AddFilters(Log, DataFolder);

            // Load the configuration.
            LoadConfiguration();
        }

        /// <summary>A factory responsible for creating a xServer logger instance.</summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>An instance of the xServer logger, which reports on the xServer's activity.</summary>
        public ILogger Logger { get; }

        /// <summary>The settings of the xServer's logger.</summary>
        public LogSettings Log { get; }

        /// <summary>
        ///     A list of paths to folders which xServer components use to store data. These folders are found
        ///     in the <see cref="DataDir" />.
        /// </summary>
        public DataFolder DataFolder { get; }

        /// <summary>
        ///     The path to the data directory, which contains, for example, the configuration file, wallet files,
        ///     and the file containing the peers that the Server has connected to. This value is read-only and can only be
        ///     set via the ServerSettings constructor's arguments.
        /// </summary>
        public string DataDir { get; }

        /// <summary>
        ///     The path to the root data directory, which holds all server data on the machine.
        ///     This includes separate subfolders for different servers that run on the machine: a xServer folder for a
        ///     xServer This value is read-only and can only be set via the ServerSettings constructor's arguments.
        /// </summary>
        public string DataDirRoot { get; }

        /// <summary>
        ///     The path to the xServer's configuration file.
        ///     This value is read-only and can only be set via the ServerSettings constructor's arguments.
        /// </summary>
        public string ConfigurationFile { get; }

        /// <summary>
        ///     A combination of the settings from the xServer's configuration file and the command
        ///     line arguments supplied to the xServer when it was run. This places the settings from both sources
        ///     into a single object, which is referenced at runtime.
        /// </summary>
        public TextFileConfiguration ConfigReader { get; }

        /// <summary>The version of the protocol supported by the xServer.</summary>
        public ProtocolVersion ProtocolVersion { get; }

        /// <summary>The lowest version of the protocol which the xServer supports.</summary>
        public ProtocolVersion? MinProtocolVersion { get; set; }

        /// <summary>The servernode which the server is configured to run on.</summary>
        public ServerNodeBase ServerNode { get; }

        /// <summary>
        ///     A string that is used to help identify the xServer when it connects to other peers on a servernode.
        ///     Defaults to "xServer".
        /// </summary>
        public string Agent { get; }

        /// <summary>Determines whether to print help and exit.</summary>
        public bool PrintHelpAndExit =>
            ConfigReader.GetOrDefault("help", false, Logger) ||
            ConfigReader.GetOrDefault("-help", false, Logger);

        /// <inheritdoc />
        public void Dispose()
        {
            LoggerFactory.Dispose();
        }

        /// <summary>
        ///     Initializes default configuration.
        /// </summary>
        /// <param name="serverNode">Specification of the xServer the server runs on</param>
        /// <param name="protocolVersion">Supported protocol version for which to create the configuration.</param>
        /// <returns>Default server configuration.</returns>
        public static ServerSettings Default(ServerNodeBase serverNode,
            ProtocolVersion protocolVersion = SupportedProtocolVersion)
        {
            return new ServerSettings(serverNode, protocolVersion);
        }

        /// <summary>
        ///     Creates the configuration file if it does not exist.
        /// </summary>
        /// <param name="features">The features for which to include settings in the configuration file.</param>
        public void CreateDefaultConfigurationFile(List<IFeatureRegistration> features)
        {
            // If the config file does not exist yet then create it now.
            if (!File.Exists(ConfigurationFile))
            {
                Logger.LogDebug("Creating configuration file '{0}'.", ConfigurationFile);

                StringBuilder builder = new StringBuilder();

                foreach (IFeatureRegistration featureRegistration in features)
                {
                    MethodInfo getDefaultConfiguration =
                        featureRegistration.FeatureType.GetMethod("BuildDefaultConfigurationFile",
                            BindingFlags.Public | BindingFlags.Static);
                    if (getDefaultConfiguration != null)
                    {
                        getDefaultConfiguration.Invoke(null, new object[] { builder, ServerNode });
                        builder.AppendLine();
                    }
                }

                File.WriteAllText(ConfigurationFile, builder.ToString());
                ReadConfigurationFile();
                LoadConfiguration();
            }
        }

        /// <summary>
        ///     Reads the configuration file and merges it with the command line arguments.
        /// </summary>
        private void ReadConfigurationFile()
        {
            Logger.LogDebug("Reading configuration file '{0}'.", ConfigurationFile);

            // Add the file configuration to the command-line configuration.
            TextFileConfiguration fileConfig = new TextFileConfiguration(File.ReadAllText(ConfigurationFile));
            fileConfig.MergeInto(ConfigReader);
        }

        /// <summary>
        ///     Loads the server settings from the application configuration.
        /// </summary>
        private void LoadConfiguration()
        {
            TextFileConfiguration config = ConfigReader;
        }

        /// <summary>
        ///     Creates default data directories respecting different operating system specifics.
        /// </summary>
        /// <param name="appName">Name of the server, which will be reflected in the name of the data directory.</param>
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
                    Logger.LogDebug("Using HOME environment variable for initializing application data.");
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
                    Logger.LogDebug("Using APPDATA environment variable for initializing application data.");
                    directoryPath = Path.Combine(localAppData, appName);
                }
                else
                {
                    throw new DirectoryNotFoundException("Could not find APPDATA directory.");
                }
            }

            // Create the data directories if they don't exist.
            Directory.CreateDirectory(directoryPath);

            Logger.LogDebug("Data directory initialized with path {0}.", directoryPath);
            return directoryPath;
        }

        /// <summary>
        ///     Displays command-line help.
        /// </summary>
        /// <param name="serverNode">The servernode to extract values from.</param>
        public static void PrintHelp(ServerNodeBase serverNode)
        {
            Guard.NotNull(serverNode, nameof(serverNode));

            ServerSettings defaults = Default(serverNode);
            string daemonName = Path.GetFileName(Assembly.GetEntryAssembly().Location);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Usage:");
            builder.AppendLine($" dotnet run {daemonName} [arguments]");
            builder.AppendLine();
            builder.AppendLine("Command line arguments:");
            builder.AppendLine();
            builder.AppendLine("-help/--help              Show this help.");
            builder.AppendLine(
                $"-conf=<Path>              Path to the configuration file. Defaults to {defaults.ConfigurationFile}.");
            builder.AppendLine(
                $"-datadir=<Path>           Path to the data directory. Defaults to {defaults.DataDir}.");
            builder.AppendLine(
                "-debug[=<string>]         Set 'Debug' logging level. Specify what to log via e.g. '-debug=X42.Protocol,X42.Server.Builder'.");
            builder.AppendLine(
                "-loglevel=<string>        Direct control over the logging level: '-loglevel=trace/debug/info/warn/error/fatal'.");

            defaults.Logger.LogInformation(builder.ToString());
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="serverNode">The servernode to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, ServerNodeBase serverNode)
        {
            builder.AppendLine("####Server Settings####");
            builder.AppendLine();
        }
    }
}