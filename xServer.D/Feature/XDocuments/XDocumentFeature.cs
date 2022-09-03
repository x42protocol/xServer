using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using x42.Configuration.Logging;
using x42.Feature.PowerDns;
using x42.Feature.Setup;
using x42.Feature.X42Client;
using x42.Server;

namespace x42.Feature.XDocuments
{
    public class XDocumentFeature : ServerFeature
    {
        private XDocumentClient _xDocumentClient;
        private readonly ILogger _logger;
        private readonly PowerDnsFeature _powerDnsFeature;
        private readonly X42Node _x42Client;

        public XDocumentFeature(ILoggerFactory loggerFactory, PowerDnsFeature powerDnsFeature, X42Node x42Client)
        {
            _logger = loggerFactory.CreateLogger(GetType().FullName);
            _powerDnsFeature = powerDnsFeature;
            _x42Client = x42Client;

            _xDocumentClient = new XDocumentClient(_powerDnsFeature, _x42Client);
        }
        public override Task InitializeAsync()
        {
            _xDocumentClient = new XDocumentClient(_powerDnsFeature, _x42Client);

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

                    });
            });

            return serverBuilder;
        }

    }
}
