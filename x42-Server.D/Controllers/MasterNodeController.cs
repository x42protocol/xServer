using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NLog;
using x42.Feature.API.Requirements;
using X42.Configuration;
using X42.Controllers.Models;
using X42.MasterNode;
using X42.Server;
using X42.Utilities;
using X42.Utilities.Extensions;
using X42.Utilities.JsonErrors;
using X42.Utilities.ModelStateErrors;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NLog.LogLevel;

namespace X42.Controllers
{
    /// <summary>
    ///     Provides methods that interact with the full node.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Policy = Policy.PrivateAccess)]
    public class MasterNodeContoller : Controller
    {
        /// <summary>Provider of date and time functions.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Masternode.</summary>
        private readonly MasterNodeBase masterNode;

        /// <summary>The settings for the node.</summary>
        private readonly ServerSettings nodeSettings;

        /// <summary>x42 Server.</summary>
        private readonly IX42Server x42Server;

        public MasterNodeContoller(IX42Server x42Server, ILoggerFactory loggerFactory,
            IDateTimeProvider dateTimeProvider,
            ServerSettings serverSettings,
            MasterNodeBase masterNode)
        {
            Guard.NotNull(x42Server, nameof(x42Server));
            Guard.NotNull(masterNode, nameof(masterNode));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(serverSettings, nameof(serverSettings));
            Guard.NotNull(dateTimeProvider, nameof(dateTimeProvider));

            this.x42Server = x42Server;
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.dateTimeProvider = dateTimeProvider;
            nodeSettings = serverSettings;
            this.masterNode = masterNode;
        }

        /// <summary>
        ///     Returns some general information about the status of the underlying node.
        /// </summary>
        /// <returns>A <see cref="StatusModel" /> with information about the node.</returns>
        [HttpGet]
        [Route("status")]
        public IActionResult Status()
        {
            // Output has been merged with RPC's GetInfo() since they provided similar functionality.
            var model = new StatusModel
            {
                Version = x42Server.Version?.ToString() ?? "0",
                ProtocolVersion = (uint) nodeSettings.ProtocolVersion,
                ProcessId = Process.GetCurrentProcess().Id,
                DataDirectoryPath = nodeSettings.DataDir,
                RunningTime = dateTimeProvider.GetUtcNow() - x42Server.StartTime,
                State = x42Server.State.ToString()
            };

            // Add the list of features that are enabled.
            foreach (IX42Server feature in x42Server.Services.Features)
                model.EnabledFeatures.Add(feature.GetType().ToString());

            return Json(model);
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
            // the x42 server RunAsync to continue processing, which calls Dispose on the node.
            x42Server?.X42ServerLifetime.StopApplication();

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
                foreach (var logRuleRequest in request.LogRules)
                {
                    var nLogLevel = logRuleRequest.LogLevel.ToNLogLevel();
                    var rule = LogManager.Configuration.LoggingRules.SingleOrDefault(r =>
                        r.LoggerNamePattern == logRuleRequest.RuleName);

                    if (rule == null) throw new Exception($"Logger name `{logRuleRequest.RuleName}` doesn't exist.");

                    // Log level ordinals go from 1 to 6 (trace to fatal).
                    // When we set a log level, we enable every log level above and disable all the ones below.
                    foreach (var level in LogLevel.AllLoggingLevels)
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