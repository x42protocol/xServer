using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using x42.Configuration.Logging;
using x42.Feature.Database;
using x42.Feature.Network;
using x42.Feature.Setup;
using x42.Feature.WordPressPreview;
using x42.Feature.WordPressPreview.Models;
using x42.Server;
namespace x42.Feature.PowerDns
{
    public class WordPressPreviewFeature : ServerFeature
    {
        private readonly ILogger _logger;
        private readonly WordPressManager _wordPressManager;
        private readonly NetworkFeatures _networkFeatures;
        public WordPressPreviewFeature(
            ILoggerFactory loggerFactory,
            WordPressManager wordPressManager,
            NetworkFeatures networkFeatures)
        {
            _logger = loggerFactory.CreateLogger(GetType().FullName);
            _wordPressManager = wordPressManager;
            _networkFeatures = networkFeatures;
        }

        public override Task InitializeAsync()
        {
             return Task.CompletedTask;

        }

        public async Task<List<string>> GetWordPressPreviewDomains() {

            return await _wordPressManager.GetWordPressPreviewDomains();
        }

        public async Task<WordpressDomainResult> ReserveWordpressPreviewDNS(WordPressReserveRequest registerRequest)
        {
            return await _wordPressManager.ReserveWordpressPreviewDNS(registerRequest);
        }

        /// <inheritdoc />
        public override void ValidateDependencies(IServerServiceProvider services)
        {
         
         
        }

      
    }   

    public static class WordPressBuilderExtension
    {
        /// <summary>
        ///     Adds PowerDns feature
        /// </summary>
        /// <param name="serverBuilder">The object used to build the current node.</param>
        /// <returns>The server builder, enriched with the new component.</returns>
        public static IServerBuilder UseWordPressPreview(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<WordPressPreviewFeature>("wordpresspreview");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<WordPressPreviewFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<WordPressPreviewFeature>();
                        services.AddSingleton<WordPressManager>();
                        services.AddSingleton<PowerDnsFeature>();
                        services.AddSingleton<NetworkFeatures>();
                        services.AddSingleton<DatabaseFeatures>();
                        services.AddSingleton<DatabaseSettings>();


                    });
            });

            return serverBuilder;
        }      
    }
}
