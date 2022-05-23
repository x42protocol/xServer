using Microsoft.Extensions.Logging;
using System.Net;
using x42.Configuration;
using x42.Feature.PowerDns.PowerDnsClient;
using x42.Utilities;

namespace x42.Feature.PowerDns
{

    /// <summary>
    ///     Configuration related to the PowerDns interface.
    /// </summary>
    public class PowerDnsSettings
    {

        /// <summary>
        ///     The PowerDns Host.
        /// </summary>
        public string PowerDnsHost { get; set; }
        public string ApiKey { get; set; }

        /// <summary>
        ///     Initializes an instance of the object from the node configuration.
        /// </summary>
        /// <param name="serverSettings">The node configuration.</param>
        /// 

        private PowerDnsRestClient powerDnsClient;


        public PowerDnsSettings(ServerSettings serverSettings)
        {
            Guard.NotNull(serverSettings, nameof(serverSettings));
            ILogger logger = serverSettings.LoggerFactory.CreateLogger(typeof(PowerDnsSettings).FullName);

            TextFileConfiguration config = serverSettings.ConfigReader;

            PowerDnsHost = config.GetOrDefault("pdnshost", "", logger);
            ApiKey = config.GetOrDefault("pdnsapikey", "", logger);

            this.powerDnsClient = new PowerDnsRestClient($"http://{PowerDnsHost}/", ApiKey, logger);
        }

     
    }
}
