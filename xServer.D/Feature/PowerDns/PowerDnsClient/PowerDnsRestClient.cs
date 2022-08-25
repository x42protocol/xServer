using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using x42.Feature.PowerDns.Models;
using RestSharp;
using Common.Models.XDocuments.DNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

        public async Task AddNewSubDomain(string subdomain)
        {
            try
            {
                var client = new RestClient(_baseUrl);
                var request = new RestRequest($"/api/v1/servers/localhost/zones/wordpresspreview.site", Method.Patch);
                request.AddHeader("X-API-Key", _apiKey);
                request.AddHeader("content-type", "application/json");

                var body = new DnsRequest() { Rrsets = new List<RRset>() { new RRset($"{subdomain}.wordpresspreview.site.", "REPLACE", 60, "A", "144.91.69.12") } };

                var response = await client.ExecuteAsync(request);
                request.AddBody(body);

            }
            catch (Exception ex)
            {
                _logger.LogDebug($"An Error Occured When add subdomain!", ex);

             }
        }

        public async Task CreateDNSZone(string zone)
        {
 
            var client = new RestClient(_baseUrl);
            var request = new RestRequest($"/api/v1/servers/localhost/zones", Method.Post);
            request.AddHeader("X-API-Key", _apiKey);
            request.AddHeader("content-type", "application/json");

            var body = new NewZoneModel(zone);

            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var json = JsonConvert.SerializeObject(body, serializerSettings);

            request.AddJsonBody(json);
            await client.ExecuteAsync(request);

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
