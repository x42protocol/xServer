using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using X42.Utilities;
using X42.Server;

namespace X42.Feature
{
    /// <summary>
    /// Starts and stops all features registered with a x42 server.
    /// </summary>
    public interface IServerFeatureExecutor : IDisposable
    {
        /// <summary>
        /// Starts all registered features of the associated x42 server.
        /// </summary>
        void Initialize();
    }

    /// <summary>
    /// Starts and stops all features registered with a x42 server.
    /// </summary>
    /// <remarks>Borrowed from ASP.NET.</remarks>
    public class ServerFeatureExecutor : IServerFeatureExecutor
    {
        /// <summary>x42 server which features are to be managed by this executor.</summary>
        private readonly IX42Server server;

        /// <summary>Object logger.</summary>
        private readonly ILogger logger;

        /// <summary>
        /// Initializes an instance of the object with specific x42 server and logger factory.
        /// </summary>
        /// <param name="server">x42 server which features are to be managed by this executor.</param>
        /// <param name="loggerFactory">Factory to be used to create logger for the object.</param>
        public ServerFeatureExecutor(IX42Server server, ILoggerFactory loggerFactory)
        {
            Guard.NotNull(server, nameof(server));

            this.server = server;
            this.logger = loggerFactory.CreateLogger(this.GetType().FullName);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            try
            {
                this.Execute(service => service.ValidateDependencies(this.server.Services));
                this.Execute(service => service.InitializeAsync().GetAwaiter().GetResult());
            }
            catch
            {
                this.logger.LogError("An error occurred starting the application.");
                this.logger.LogTrace("(-)[INITIALIZE_EXCEPTION]");
                throw;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            try
            {
                this.Execute(feature => feature.Dispose(), true);
            }
            catch
            {
                this.logger.LogError("An error occurred stopping the application.");
                this.logger.LogTrace("(-)[DISPOSE_EXCEPTION]");
                throw;
            }
        }

        /// <summary>
        /// Executes start or stop method of all the features registered with the associated x42 server.
        /// </summary>
        /// <param name="callback">Delegate to run start or stop method of the feature.</param>
        /// <param name="disposing">Reverse the order of which the features are executed.</param>
        /// <exception cref="AggregateException">Thrown in case one or more callbacks threw an exception.</exception>
        private void Execute(Action<IServerFeature> callback, bool disposing = false)
        {
            if (this.server.Services == null)
            {
                this.logger.LogTrace("(-)[NO_SERVICES]");
                return;
            }

            List<Exception> exceptions = null;

            if (disposing)
            {
                // When the server is shutting down, we need to dispose all features, so we don't break on exception.
                foreach (IServerFeature feature in this.server.Services.Features.Reverse())
                {
                    try
                    {
                        callback(feature);
                    }
                    catch (Exception exception)
                    {
                        if (exceptions == null)
                            exceptions = new List<Exception>();

                        this.LogAndAddException(exceptions, exception);
                    }
                }
            }
            else
            {
                // When the server is starting we don't continue initialization when an exception occurs.
                try
                {
                    // Initialize features that are flagged to start before the base feature.
                    foreach (IServerFeature feature in this.server.Services.Features.OrderByDescending(f => f.InitializeBeforeBase))
                    {
                        callback(feature);
                    }
                }
                catch (Exception exception)
                {
                    if (exceptions == null)
                        exceptions = new List<Exception>();

                    this.LogAndAddException(exceptions, exception);
                }
            }

            // Throw an aggregate exception if there were any exceptions.
            if (exceptions != null)
            {
                this.logger.LogTrace("(-)[EXECUTION_FAILED]");
                throw new AggregateException(exceptions);
            }
        }

        private void LogAndAddException(List<Exception> exceptions, Exception exception)
        {
            exceptions.Add(exception);

            this.logger.LogError("An error occurred: '{0}'", exception.ToString());
        }
    }
}
