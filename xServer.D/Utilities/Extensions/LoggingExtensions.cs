using System;
using Microsoft.Extensions.Logging;
using LogLevel = NLog.LogLevel;

namespace x42.Utilities.Extensions
{
    /// <summary>
    ///     Extension methods for classes and interfaces related to logging.
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        ///     Creates a new <see cref="ILogger" /> instance which prefixes every log with specified string.
        /// </summary>
        /// <param name="loggerFactory">Logger factory interface being extended.</param>
        /// <param name="categoryName">Category name for messages produced by the logger.</param>
        /// <param name="prefix">String to be put in front of each log of the newly created logger.</param>
        /// <returns>Newly created logger.</returns>
        public static ILogger CreateLogger(this ILoggerFactory loggerFactory, string categoryName, string prefix = "")
        {
            return new PrefixLogger(loggerFactory, categoryName, prefix);
        }

        /// <summary>
        ///     Converts <see cref="Microsoft.Extensions.Logging.LogLevel" /> to <see cref="NLog.LogLevel" />.
        /// </summary>
        /// <param name="logLevel">Log level value to convert.</param>
        /// <returns>NLog value of the log level.</returns>
        public static LogLevel ToNLogLevel(this Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            LogLevel res = LogLevel.Trace;

            switch (logLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                    res = LogLevel.Trace;
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Debug:
                    res = LogLevel.Debug;
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Information:
                    res = LogLevel.Info;
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    res = LogLevel.Warn;
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Error:
                    res = LogLevel.Error;
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Critical:
                    res = LogLevel.Fatal;
                    break;
            }

            return res;
        }

        /// <summary>
        ///     Converts a string to a <see cref="NLog.LogLevel" />.
        /// </summary>
        /// <param name="logLevel">Log level value to convert.</param>
        /// <returns>NLog value of the log level.</returns>
        public static LogLevel ToNLogLevel(this string logLevel)
        {
            logLevel = logLevel.ToLowerInvariant();

            switch (logLevel)
            {
                case "trace":
                    return LogLevel.Trace;
                case "debug":
                    return LogLevel.Debug;
                case "info":
                case "information":
                    return LogLevel.Info;
                case "warn":
                case "warning":
                    return LogLevel.Warn;
                case "error":
                    return LogLevel.Error;
                case "fatal":
                case "critical":
                case "crit":
                    return LogLevel.Fatal;
                case "off":
                    return LogLevel.Off;
                default:
                    throw new Exception($"Failed converting {logLevel} to a member of NLog.LogLevel.");
            }
        }
    }
}