using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using x42.Configuration;
using x42.Controllers.Results;
using x42.Controllers.Requests;
using x42.Feature.Setup;
using x42.Server;
using x42.Utilities;
using x42.Utilities.Extensions;
using x42.Utilities.JsonErrors;
using x42.Utilities.ModelStateErrors;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NLog.LogLevel;
using x42.Feature.Database;
using x42.Server.Results;
using System.Threading.Tasks;
using Common.Models.XServer;

namespace x42.Controllers
{
    /// <inheritdoc />
    /// <summary>
    ///     Provides methods that interact with the full node.
    /// </summary>
    [ApiController]
    [Route("")]
    //[Authorize(Policy = Policy.PrivateAccess)]
    public class ServerNodeContoller : Controller
    {
        /// <summary>Provider of date and time functions.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>The settings for the node.</summary>
        private readonly ServerSettings nodeSettings;

        /// <summary>xServer.</summary>
        private readonly IxServer xServer;

        /// <summary>Database details.</summary>
        private readonly DatabaseFeatures databaseFeatures;

        public ServerNodeContoller(IxServer xServer, ILoggerFactory loggerFactory,
            IDateTimeProvider dateTimeProvider,
            ServerSettings serverSettings,
            DatabaseFeatures databaseFeatures)
        {
            Guard.NotNull(xServer, nameof(xServer));
            Guard.NotNull(loggerFactory, nameof(loggerFactory));
            Guard.NotNull(serverSettings, nameof(serverSettings));
            Guard.NotNull(dateTimeProvider, nameof(dateTimeProvider));
            Guard.NotNull(databaseFeatures, nameof(databaseFeatures));

            this.xServer = xServer;
            logger = loggerFactory.CreateLogger(GetType().FullName);
            this.dateTimeProvider = dateTimeProvider;
            nodeSettings = serverSettings;
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
            Results.StatusResult model = new Results.StatusResult
            {
                Version = xServer.Version?.ToString() ?? "0",
                ProtocolVersion = (uint)nodeSettings.ProtocolVersion,
                ProcessId = Process.GetCurrentProcess().Id,
                DataDirectoryPath = nodeSettings.DataDir,
                RunningTimeSeconds = (dateTimeProvider.GetUtcNow() - xServer.StartTime).TotalSeconds,
                State = xServer.State.ToString(),
                DatabaseConnected = databaseFeatures.DatabaseConnected,
                Stats = xServer.Stats,
                FeeAddress = xServer.GetMyFeeAddress(),
                Name = xServer.GetServerProfileName(),
                PublicKey = xServer.GetMyPublicKey()
            };

            // Add the list of features that are enabled.
            foreach (IServerFeature feature in xServer.Services.Features)
                model.EnabledFeatures.Add(feature.GetType().ToString());

            return Json(model);
        }

        /// <summary>
        ///     Prepares the server address.
        /// </summary>
        /// <returns>
        ///     <see cref="SetupResponse" />
        /// </returns>
        [HttpGet]
        [Route("setup")]
        public async Task<IActionResult> Setup()
        {
            string address = await xServer.SetupServer();
            var setupResult = new SetupResponse()
            {
                SignAddress = address
            };
            return Json(setupResult);
        }

        /// <summary>
        ///     Prepares the server to get ready to setup.
        /// </summary>
        /// <returns>
        ///     <see cref="OkResult" />
        /// </returns>
        [HttpPost]
        [Route("set-server-address")]
        public async Task<IActionResult> SetServerAddress([FromBody] SetupRequest setupRequest)
        {
            string result = await xServer.SetupServer(setupRequest);
            var setupResult = new SetupResponse()
            {
                SignAddress = result
            };
            return Json(setupResult);
        }

        /// <summary>
        ///     Get the status of the sever setup.
        /// </summary>
        /// <returns>A <see cref="SetupStatusResult" /> with information about the node.</returns>
        [HttpGet]
        [Route("getserversetupstatus")]
        public IActionResult GetServerSetupStatus()
        {
            SetupStatusResult result = xServer.GetServerSetupStatus();
            return Json(result);
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
        public IActionResult Shutdown([FromBody] bool corsProtection = true)
        {
            // Start the node shutdown process, by calling StopApplication, which will signal to
            // the xServer RunAsync to continue processing, which calls Dispose on the node.
            xServer?.xServerLifetime.StopApplication();

            return Ok();
        }

        /// <summary>
        ///     Starts the xServer
        /// </summary>
        /// <returns>
        ///     <see cref="OkResult" />
        /// </returns>
        [HttpPost]
        [Route("start")]
        public IActionResult Start([FromBody] StartRequest startRequest)
        {
            xServer.Start(startRequest);
            return Ok();
        }

        /// <summary>
        ///     Stops the xServer
        /// </summary>
        /// <returns>
        ///     <see cref="OkResult" />
        /// </returns>
        [HttpPost]
        [Route("stop")]
        public IActionResult Stop()
        {
            xServer.Stop();
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