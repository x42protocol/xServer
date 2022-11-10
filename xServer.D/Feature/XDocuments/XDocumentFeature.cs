using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using x42.Configuration.Logging;
using x42.Feature.PowerDns;
using x42.Feature.Setup;
using x42.Feature.X42Client;
using x42.Server;
using x42.Utilities;

namespace x42.Feature.XDocuments
{
    public class XDocumentFeature : ServerFeature
    {
        private XDocumentClient _xDocumentClient;
        private readonly PowerDnsFeature _powerDnsFeature;
        private X42ClientSettings _x42ClientSettings;
        private readonly IxServerLifetime _serverLifetime;
        private readonly ILogger _logger;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory _asyncLoopFactory;


        public XDocumentFeature(ILoggerFactory loggerFactory, PowerDnsFeature powerDnsFeature, X42ClientSettings x42ClientSettings, IxServerLifetime serverLifetime, IAsyncLoopFactory asyncLoopFactory)
        {

            _x42ClientSettings = x42ClientSettings;
            _logger = loggerFactory.CreateLogger(GetType().FullName);
            _serverLifetime = serverLifetime;
            _asyncLoopFactory = asyncLoopFactory;


            _xDocumentClient = new XDocumentClient(_powerDnsFeature, _x42ClientSettings, loggerFactory, _serverLifetime, _asyncLoopFactory);
        }
        public override Task InitializeAsync()
        {
           // _xDocumentClient = new XDocumentClient(_powerDnsFeature, _x42ClientSettings, _logger, _serverLifetime, _asyncLoopFactory);

            return Task.CompletedTask;

        }

    }

    public static class XDocumentBuilderExtension
    {


        public static IServerBuilder UseXDocuments(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<XDocumentFeature>("xDocuments");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<XDocumentFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<XDocumentFeature>();
                        services.AddSingleton<XDocumentClient>();
                        services.AddSingleton<PowerDnsFeature>();
                        services.AddSingleton<X42ClientFeature>();
                        services.AddSingleton<X42ClientSettings>();
 

                    });
            });

            return serverBuilder;
        }

    }
}
