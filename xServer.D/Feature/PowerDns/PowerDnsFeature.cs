using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using x42.Configuration.Logging;
using x42.Feature.Network;
using x42.Feature.PowerDns.Models;
using x42.Feature.PowerDns.PowerDnsClient;
using x42.Feature.Setup;
using x42.Server;
namespace x42.Feature.PowerDns
{
    public class PowerDnsFeature : ServerFeature
    {
        private  PowerDnsRestClient _powerDnsRestClient;
        private readonly ILogger _logger;
        private readonly PowerDnsSettings _powerDnsSettings;
        private readonly NetworkFeatures _networkFeatures;

        public PowerDnsFeature(ILoggerFactory loggerFactory, PowerDnsSettings powerDnsSettings, NetworkFeatures networkFeatures)
        {
            _powerDnsSettings = powerDnsSettings;
            _logger = loggerFactory.CreateLogger(GetType().FullName);
            _powerDnsRestClient = new PowerDnsRestClient(_powerDnsSettings.PowerDnsHost, _powerDnsSettings.ApiKey, _logger);
            _networkFeatures = networkFeatures;
        }

        public override Task InitializeAsync()
        {
            _powerDnsRestClient = new PowerDnsRestClient(_powerDnsSettings.PowerDnsHost, _powerDnsSettings.ApiKey, _logger);
            return Task.CompletedTask;

        }

        /// <inheritdoc />
        public override void ValidateDependencies(IServerServiceProvider services)
        {
         
         
        }

        public async Task<List<ZoneModel>> GetAllZones()
        {
           return await _powerDnsRestClient.GetAllZones();
        }

        public async Task AddNewSubDomain(string name) { 
        
            await _powerDnsRestClient.AddNewSubDomain(name);
        
        }

       

    }   

    public static class DnsBuilderExtension
    {
        /// <summary>
        ///     Adds PowerDns feature
        /// </summary>
        /// <param name="serverBuilder">The object used to build the current node.</param>
        /// <returns>The server builder, enriched with the new component.</returns>
        public static IServerBuilder UsePowerDns(this IServerBuilder serverBuilder)
        {
            LoggingConfiguration.RegisterFeatureNamespace<PowerDnsFeature>("powerdns");

            serverBuilder.ConfigureFeature(features =>
            {
                features
                    .AddFeature<PowerDnsFeature>()
                    .FeatureServices(services =>
                    {
                        services.AddSingleton<PowerDnsFeature>();
                        services.AddSingleton<PowerDnsSettings>();
 
                    });
            });

            return serverBuilder;
        }      
    }
}
