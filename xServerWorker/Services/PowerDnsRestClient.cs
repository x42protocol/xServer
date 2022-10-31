using Common.Models.XDocuments.DNS;
using Common.Models.XDocuments.Zones;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;

namespace xServerWorker.Services
{
    public class PowerDnsRestClient
    {


        private readonly MongoClient _client;
        private readonly IMongoDatabase _db;
        private static string _powerDnsHost = "";
        private static string _powerDnsApiKey = "";
        private static string _xServerHost = "http://x42server:4242/";
        private static string _feeAddress = "";

        public PowerDnsRestClient()
        {
            var mongoConnectionString = Environment.GetEnvironmentVariable("MONGOCONNECTIONSTRING");

            _powerDnsHost = Environment.GetEnvironmentVariable("POWERDNSHOST") ?? "";
            _powerDnsApiKey = Environment.GetEnvironmentVariable("PDNS_API_KEY") ?? "";



#if DEBUG
            _powerDnsHost = "https://poweradmin2.xserver.network";
            _powerDnsApiKey = "anltTlNVTE9IYTdVc09u";
            _xServerHost = "http://127.0.0.1:4242/";
            _client = new MongoClient($"mongodb://localhost:27017");


#else
            _client = new MongoClient(mongoConnectionString);
            //_xServerHost = Environment.GetEnvironmentVariable("XSERVER_BACKEND") ?? "";

#endif



            _db = _client.GetDatabase("xServerDb");

        }


        public async Task CreateDNSZone(dynamic dynamicObject, int blockHeight)
        {

            string zone = (string)dynamicObject["data"]["zone"];
            var zoneDocumentCollection = _db.GetCollection<BsonDocument>("DnsZones");

            var filter = Builders<BsonDocument>.Filter.Eq("zone", zone);
            var zoneDocument = zoneDocumentCollection.Find(filter).FirstOrDefault();


            if (zoneDocument == null)
            {

                var client = new RestClient(_powerDnsHost);
                var request = new RestRequest($"/api/v1/servers/localhost/zones", Method.Post);
                request.AddHeader("X-API-Key", _powerDnsApiKey);
                request.AddHeader("content-type", "application/json");

                var body = new NewZoneModel(zone);

                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                var json = JsonConvert.SerializeObject(body, serializerSettings);

                request.AddJsonBody(json);

                try
                {
                    var response = await client.ExecuteAsync(request);

                }
                catch (Exception e)
                {

                    throw;
                }


                var newZoneDocument = JsonConvert.DeserializeObject<dynamic>("{}");

                var Id = Guid.NewGuid();


                newZoneDocument._id = Id;
                newZoneDocument.zone = zone;
                newZoneDocument.keyAddress = dynamicObject["keyAddress"];
                newZoneDocument.signature = dynamicObject["signature"];
                newZoneDocument.pricelockId = dynamicObject["priceLockId"];
                newZoneDocument.blockConfirmed = blockHeight;
                newZoneDocument.rrSets = null;

                dynamic insertZoneDocument = GetBsonFromDynamic(newZoneDocument);

                zoneDocumentCollection.InsertOne(insertZoneDocument);


            }

            else
            {

                throw new Exception("Owned Elsewhere");

            }




        }

        public async Task UpdateDNSZone(dynamic dynamicObject, int blockHeight, List<RrSet> rrSets)
        {

            string zone = (string)dynamicObject["data"]["zone"];
            var zoneDocumentCollection = _db.GetCollection<BsonDocument>("DnsZones");

            var filter = Builders<BsonDocument>.Filter.Eq("zone", zone);
            var zoneDocument = zoneDocumentCollection.Find(filter).FirstOrDefault();


            if (zoneDocument == null)
            {

                var client = new RestClient(_powerDnsHost);
                var request = new RestRequest($"/api/v1/servers/localhost/zones/"+ zone, Method.Patch);
                request.AddHeader("X-API-Key", _powerDnsApiKey);
                request.AddHeader("content-type", "application/json");

 
                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                var json = JsonConvert.SerializeObject(rrSets, serializerSettings);

                request.AddJsonBody(json);
                await client.ExecuteAsync(request);


                var updateZoneDocument = JsonConvert.DeserializeObject<dynamic>(zoneDocument.ToString());

                var Id = Guid.NewGuid();


                updateZoneDocument._id = Id;
                updateZoneDocument.blockConfirmed = blockHeight;
                updateZoneDocument.rrSets = rrSets;

                dynamic insertZoneDocument = GetBsonFromDynamic(updateZoneDocument);

                zoneDocumentCollection.InsertOne(insertZoneDocument);


            }

            else
            {

                throw new Exception("Owned Elsewhere");

            }




        }
        private static dynamic GetBsonFromDynamic(dynamic newZoneDocument)
        {
            var jsonstring = JsonConvert.SerializeObject(newZoneDocument);
            var insertZoneDocument = BsonSerializer.Deserialize<BsonDocument>(jsonstring);
            return insertZoneDocument;
        }
    }
}
