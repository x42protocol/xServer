using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using X42.Server;
using X42.Utilities;

namespace X42.Feature.Setup
{
    /// <summary>
    ///     Starts and stops all features registered with a x42 server.
    /// </summary>
    public interface IServerFeatureExecutor : IDisposable
    {
        /// <summary>
        ///     Starts all registered features of the associated x42 server.
        /// </summary>
        void Initialize();
    }

    /// <summary>
    ///     Starts and stops all features registered with a x42 server.
    /// </summary>
    /// <remarks>Borrowed from ASP.NET.</remarks>
    public class ServerFeatureExecutor : IServerFeatureExecutor
    {
        /// <summary>Object logger.</summary>
        private readonly ILogger logger;

        /// <summary>x42 server which features are to be managed by this executor.</summary>
        private readonly IX42Server server;

        /// <summary>
        ///     Initializes an instance of the object with specific x42 server and logger factory.
        /// </summary>
        /// <param name="server">x42 server which features are to be managed by this executor.</param>
        /// <param name="loggerFactory">Factory to be used to create logger for the object.</param>
        public ServerFeatureExecutor(IX42Server server, ILoggerFactory loggerFactory)
        {
            Guard.NotNull(server, nameof(server));

            this.server = server;
            logger = loggerFactory.CreateLogger(GetType().FullName);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            try
            {
                Execute(service => service.ValidateDependencies(server.Services));
                Execute(service => service.InitializeAsync().GetAwaiter().GetResult());
            }
            catch
            {
                logger.LogError("An error occurred starting the application.");
                logger.LogTrace("(-)[INITIALIZE_EXCEPTION]");
                throw;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            try
            {
                Execute(feature => feature.Dispose(), true);
            }
            catch
            {
                logger.LogError("An error occurred stopping the application.");
                logger.LogTrace("(-)[DISPOSE_EXCEPTION]");
                throw;
            }
        }

        /// <summary>
        ///     Executes start or stop method of all the features registered with the associated x42 server.
        /// </summary>
        /// <param name="callback">Delegate to run start or stop method of the feature.</param>
        /// <param name="disposing">Reverse the order of which the features are executed.</param>
        /// <exception cref="AggregateException">Thrown in case one or more callbacks threw an exception.</exception>
        private void Execute(Action<IServerFeature> callback, bool disposing = false)
        {
            if (server.Services == null)
            {
                logger.LogTrace("(-)[NO_SERVICES]");
                return;
            }

            List<Exception> exceptions = null;

            if (disposing)
                foreach (var feature in server.Services.Features.Reverse())
                    try
                    {
                        callback(feature);
                    }
                    catch (Exception exception)
                    {
                        if (exceptions == null)
                            exceptions = new List<Exception>();

                        LogAndAddException(exceptions, exception);
                    }
            else
                try
                {
                    // Initialize features that are flagged to start before the base feature.
                    foreach (var feature in server.Services.Features.OrderByDescending(f => f.InitializeBeforeBase))
                        callback(feature);
                }
                catch (Exception exception)
                {
                    if (exceptions == null)
                        exceptions = new List<Exception>();

                    LogAndAddException(exceptions, exception);
                }

            // Throw an aggregate exception if there were any exceptions.
            if (exceptions != null)
            {
                logger.LogTrace("(-)[EXECUTION_FAILED]");
                throw new AggregateException(exceptions);
            }
        }

        private void LogAndAddException(List<Exception> exceptions, Exception exception)
        {
            exceptions.Add(exception);

            logger.LogError("An error occurred: '{0}'", exception.ToString());
        }
    }
}