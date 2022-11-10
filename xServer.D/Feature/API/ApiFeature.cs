using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using x42.Feature.Setup;
using x42.ServerNode;
using x42.Server;
using Common.Services;

namespace x42.Feature.Api
{
    /// <summary>
    ///     Provides an Api to the xServer
    /// </summary>
    public sealed class ApiFeature : ServerFeature
    {
        /// <summary>How long we are willing to wait for the API to stop.</summary>
        private const int ApiStopTimeoutSeconds = 10;

        private readonly ApiFeatureOptions apiFeatureOptions;

        private readonly ApiSettings apiSettings;

        private readonly ICertificateStore certificateStore;

        private readonly ILogger logger;

        private readonly IServerBuilder serverBuilder;

        private readonly XServer xServer;

        private IWebHost webHost;

        public ApiFeature(
            IServerBuilder xServerBuilder,
            XServer xServer,
            ApiFeatureOptions apiFeatureOptions,
            ApiSettings apiSettings,
            ILoggerFactory loggerFactory,
            ICertificateStore certificateStore)
        {
            serverBuilder = xServerBuilder;
            this.xServer = xServer;
            this.apiFeatureOptions = apiFeatureOptions;
            this.apiSettings = apiSettings;
            this.certificateStore = certificateStore;
            logger = loggerFactory.CreateLogger(GetType().FullName);
        }

        public override Task InitializeAsync()
        {
            logger.LogInformation("API starting on URL '{0}'.", apiSettings.ApiUri);
            webHost = ApiBuilder.Initialize(serverBuilder.Services, xServer, apiSettings, certificateStore,
                new WebHostBuilder());

            if (apiSettings.KeepaliveTimer == null)
            {
                logger.LogTrace("(-)[KEEPALIVE_DISABLED]");
                return Task.CompletedTask;
            }

            // Start the keepalive timer, if set.
            // If the timer expires, the node will shut down.
            apiSettings.KeepaliveTimer.Elapsed += (sender, args) =>
            {
                logger.LogInformation("The application will shut down because the keepalive timer has elapsed.");

                apiSettings.KeepaliveTimer.Stop();
                apiSettings.KeepaliveTimer.Enabled = false;
                xServer.xServerLifetime.StopApplication();
            };

            apiSettings.KeepaliveTimer.Start();

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Prints command-line help.
        /// </summary>
        /// <param name="network">The network to extract values from.</param>
        public static void PrintHelp(ServerNodeBase network)
        {
            ApiSettings.PrintHelp(network);
        }

        /// <summary>
        ///     Get the default configuration.
        /// </summary>
        /// <param name="builder">The string builder to add the settings to.</param>
        /// <param name="network">The network to base the defaults off.</param>
        public static void BuildDefaultConfigurationFile(StringBuilder builder, ServerNodeBase network)
        {
            ApiSettings.BuildDefaultConfigurationFile(builder, network);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            // Make sure the timer is stopped and disposed.
            if (apiSettings.KeepaliveTimer != null)
            {
                apiSettings.KeepaliveTimer.Stop();
                apiSettings.KeepaliveTimer.Enabled = false;
                apiSettings.KeepaliveTimer.Dispose();
            }

            // Make sure we are releasing the listening ip address / port.
            if (webHost != null)
            {
                logger.LogInformation("API stopping on URL '{0}'.", apiSettings.ApiUri);
                webHost.StopAsync(TimeSpan.FromSeconds(ApiStopTimeoutSeconds)).Wait();
                webHost = null;
            }
        }
    }

    public sealed class ApiFeatureOptions
    {
    }

    /// <summary>
    ///     A class providing extension methods for <see cref="IServerBuilder" />.
    /// </summary>
    public static class ApiFeatureExtension
    {
        public static IServerBuilder UseApi(this IServerBuilder serverBuilder,
            Action<ApiFeatureOptions> optionsAction = null)
        {
            // TODO: move the options in to the feature builder
            ApiFeatureOptions options = new ApiFeatureOptions();
            optionsAction?.Invoke(options);

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<ApiFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton(serverBuilder);
                        services.AddSingleton(options);
                        services.AddSingleton<ApiSettings>();
                        services.AddSingleton<ICertificateStore, CertificateStore>();
                        services.AddSingleton<IDappProvisioner, DappProvisioner>();

                    });
            });

            return serverBuilder;
        }
    }
}