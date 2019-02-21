using System.Collections.Generic;
using System.Linq;
using NLog;

namespace X42.Configuration.Settings
{
    /// <summary>
    ///     Configuration related to logging.
    /// </summary>
    public class LogSettings
    {
        /// <summary>
        ///     Initializes an instance of the object with default values.
        /// </summary>
        public LogSettings()
        {
            DebugArgs = new List<string>();
            LogLevel = LogLevel.Info;
        }

        /// <summary>List of categories to enable debugging information for.</summary>
        /// <remarks>A special value of "1" of the first category enables trace level debugging information for everything.</remarks>
        public List<string> DebugArgs { get; private set; }

        /// <summary>Level of logging details.</summary>
        public LogLevel LogLevel { get; private set; }

        /// <summary>
        ///     Loads the logging settings from the application configuration.
        /// </summary>
        /// <param name="config">Application configuration.</param>
        public void Load(TextFileConfiguration config)
        {
            DebugArgs = config.GetOrDefault("debug", string.Empty).Split(',').Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            // Get the minimum log level. The default is either Information or Debug depending on the DebugArgs.
            LogLevel = DebugArgs.Any() ? LogLevel.Debug : LogLevel.Info;

            var logLevelArg = config.GetOrDefault("loglevel", string.Empty);
            if (!string.IsNullOrEmpty(logLevelArg))
                LogLevel = LogLevel.FromString(logLevelArg);
        }
    }
}