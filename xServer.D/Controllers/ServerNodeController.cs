using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using X42.Feature.API.Requirements;
using X42.Configuration;
using X42.Controllers.Models;
using X42.Controllers.Requests;
using X42.Feature.Setup;
using X42.ServerNode;
using X42.Server;
using X42.Utilities;
using X42.Utilities.Extensions;
using X42.Utilities.JsonErrors;
using X42.Utilities.ModelStateErrors;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NLog.LogLevel;
using X42.Feature.Database;
using X42.Feature.Database.Tables;

namespace X42.Controllers
{
    /// <inheritdoc />
    /// <summary>
    ///     Provides methods that interact with the full node.
    /// </summary>
    [ApiController]
    [Route("")]
    [Authorize(Policy = Policy.PrivateAccess)]
    public class ServerNodeContoller : Controller
    {
        /// <summary>Provider of date and time functions.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>ServerNode.</summary>
        private readonly ServerNodeBase serverNodeBase;

        /// <summary>The settings for the node.</summary>
        private readonly ServerSettings nodeSettings;

        /// <summary>xServer.</summary>
        private readonly IxServer xServer;

        /// <summary>Database details.</summary>
        private readonly DatabaseFeatures databaseFeatures;

        public ServerNodeContoller(IxServer xServer, ILoggerFactory loggerFactory,
            IDateTimeProvider dateTimeProvider,
            ServerSettings serverSettings,
            ServerNodeBase serverNode,
            DatabaseFeatures databaseFeatures)
        {
            Guard.NotNull(xServer, nameof(xServer));
            Guard.NotNull(serverNode, nameof(serverNode));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(serverSettings, nameof(serverSettings));
            Guard.NotNull(dateTimeProvider, nameof(dateTimeProvider));
            Guard.NotNull(databaseFeatures, nameof(databaseFeatures));

            this.xServer = xServer;
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.dateTimeProvider = dateTimeProvider;
            nodeSettings = serverSettings;
            this.serverNodeBase = serverNode;
            this.databaseFeatures = databaseFeatures;
        }

        /// <summary>
        ///     Returns some general information about the status of the underlying node.
        /// </summary>
        /// <returns>A <see cref="StatusResult" /> with information about the node.</returns>
        [HttpGet]
        [Route("status")]
        public IActionResult Status()
        {
            StatusResult model = new StatusResult
            {
                Version = xServer.Version?.ToString() ?? "0",
                ProtocolVersion = (uint)nodeSettings.ProtocolVersion,
                ProcessId = Process.GetCurrentProcess().Id,
                DataDirectoryPath = nodeSettings.DataDir,
                RunningTime = dateTimeProvider.GetUtcNow() - xServer.StartTime,
                State = xServer.State.ToString(),
                DatabaseConnected = databaseFeatures.DatabaseConnected
            };

            // Add the list of features that are enabled.
            foreach (IServerFeature feature in xServer.Services.Features)
                model.EnabledFeatures.Add(feature.GetType().ToString());

            return Json(model);
        }

        /// <summary>
        ///     Prepares the server to get ready to setup.
        /// </summary>
        /// <returns>
        ///     <see cref="OkResult" />
        /// </returns>
        [HttpPost]
        [Route("setup")]
        public IActionResult Setup([FromBody] SetupRequest setupRequest)
        {
            xServer.AddServerToSetup(new ServerData()
            {
                PublicAddress = setupRequest.Address,
                DateAdded = DateTime.UtcNow
            });

            return Ok();
        }

        /// <summary>
        ///     Triggers a shutdown of the currently running node.
        /// </summary>
        /// <param name="corsProtection">This body parameter is here to prevent a CORS call from triggering method execution.</param>
        /// <remarks>
        ///     <seealso cref="https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS#Simple_requests" />
        /// </remarks>
        /// <returns>
        ///     <see cref="OkResult" />
        /// </returns>
        [HttpPost]
        [Route("shutdown")]
        [Route("stop")]
        public IActionResult Shutdown([FromBody] bool corsProtection = true)
        {
            // Start the node shutdown process, by calling StopApplication, which will signal to
            // the xServer RunAsync to continue processing, which calls Dispose on the node.
            xServer?.xServerLifetime.StopApplication();

            return Ok();
        }

        /// <summary>
        ///     Changes the log levels for the specified loggers.
        /// </summary>
        /// <param name="request">The request containing the loggers to modify.</param>
        /// <returns>
        ///     <see cref="OkResult" />
        /// </returns>
        [HttpPut]
        [Route("loglevels")]
        public IActionResult UpdateLogLevel([FromBody] LogRulesRequest request)
        {
            Guard.NotNull(request, nameof(request));

            // Checks the request is valid.
            if (!ModelState.IsValid) return ModelStateErrors.BuildErrorResponse(ModelState);

            try
            {
                foreach (LogRuleRequest logRuleRequest in request.LogRules)
                {
                    LogLevel nLogLevel = logRuleRequest.LogLevel.ToNLogLevel();
                    LoggingRule rule = LogManager.Configuration.LoggingRules.SingleOrDefault(r =>
                        r.LoggerNamePattern == logRuleRequest.RuleName);

                    if (rule == null) throw new Exception($"Logger name `{logRuleRequest.RuleName}` doesn't exist.");

                    // Log level ordinals go from 1 to 6 (trace to fatal).
                    // When we set a log level, we enable every log level above and disable all the ones below.
                    foreach (LogLevel level in LogLevel.AllLoggingLevels)
                        if (level.Ordinal >= nLogLevel.Ordinal)
                            rule.EnableLoggingForLevel(level);
                        else
                            rule.DisableLoggingForLevel(level);
                }

                // Only update the loggers if the setting was successful.
                LogManager.ReconfigExistingLoggers();
                return Ok();
            }
            catch (Exception e)
            {
                logger.LogError("Exception occurred: {0}", e.ToString());
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, e.Message, e.ToString());
            }
        }
    }
}