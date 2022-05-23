using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using x42.Feature.PowerDns.Models;
using RestSharp;

namespace x42.Feature.PowerDns.PowerDnsClient
{
    public partial class PowerDnsRestClient
    {

        /// <summary>Instance logger.</summary>
        private readonly ILogger _logger;
        private string _baseUrl;
        private string _apiKey;

        public PowerDnsRestClient(string baseUrl, string apiKey, ILogger mainLogger)
        {
            _logger = mainLogger;
            _baseUrl = baseUrl;
            _apiKey = apiKey;

        }

        /// <summary>
        ///     Gets All Zones
        /// </summary>
        public async Task<List<ZoneModel>> GetAllZones()
        {
            try
            {
                var client = new RestClient(_baseUrl);
                var request = new RestRequest("/api/v1/servers/localhost/zones", Method.Get);
                request.AddHeader("X-API-Key", _apiKey);
                var response = await client.ExecuteAsync<List<ZoneModel>>(request);

                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An Error Occured When looking up zones!", ex);

                return null;
            }
        }



        private async Task PatchRecord(string zone, string value, string recordType, string content)
        {
            try
            {
                var client = new RestClient(_baseUrl);
                var request = new RestRequest($"/api/v1/servers/localhost/zones/{zone}", Method.Patch);

                request.AddHeader("X-API-Key", _apiKey);
                request.AddBody(new RRset($"{zone}.{value}.", "REPLACE", 60, recordType, content));
 
                var response = await client.ExecuteAsync<List<ZoneModel>>(request);


            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An Error Occured When looking up zones!", ex);

             }
        }
    }
}
