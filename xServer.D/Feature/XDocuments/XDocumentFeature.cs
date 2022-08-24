using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using x42.Configuration.Logging;
using x42.Feature.PowerDns;
using x42.Feature.Setup;
using x42.Server;

namespace x42.Feature.XDocuments
{
    public class XDocumentFeature : ServerFeature
    {
        private XDocumentClient _xDocumentClient;
        private readonly ILogger _logger;
        private readonly PowerDnsFeature _powerDnsFeature;

        public XDocumentFeature(ILoggerFactory loggerFactory, PowerDnsFeature powerDnsFeature)
        {
            _logger = loggerFactory.CreateLogger(GetType().FullName);
            _powerDnsFeature = powerDnsFeature;
            _xDocumentClient = new XDocumentClient(_powerDnsFeature);

        }
        public override Task InitializeAsync()
        {
            _xDocumentClient = new XDocumentClient(_powerDnsFeature);

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

                    });
            });

            return serverBuilder;
        }

    }
}
