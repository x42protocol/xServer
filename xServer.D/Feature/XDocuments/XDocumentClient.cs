using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using x42.Feature.PowerDns;
using x42.Feature.PowerDns.PowerDnsClient;
using x42.Feature.X42Client.RestClient;

namespace x42.Feature.XDocuments
{
    public class XDocumentClient
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _db;
        private readonly PowerDnsFeature _powerDnsService;


        public XDocumentClient(PowerDnsFeature powerDnsService)
        {
            var conventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("camelCase", conventionPack, t => true);



            var mongoUser = Environment.GetEnvironmentVariable("MONGO_USER");
            var mongoPassword = Environment.GetEnvironmentVariable("MONGO_PASSWORD");

            //  _client = new MongoClient($"mongodb://{mongoUser}:{mongoPassword}@xDocumentStore:27017/");

            _client = new MongoClient($"mongodb://localhost:27017");



            _db = _client.GetDatabase("xServerDb");
            _powerDnsService = powerDnsService;
        }

        public async Task<object> GetDocumentById(Guid Id)
        {


            // Get xDocument Collection Reference
            var xDocumentCollection = _db.GetCollection<BsonDocument>("xDocument");

            var filter = Builders<BsonDocument>.Filter.Eq("_id", Id.ToString());
            var document = xDocumentCollection.Find(filter).FirstOrDefault();

            var dynamicObject = JsonConvert.DeserializeObject<dynamic>(document.ToString());


            // Convert request to JSON string

            return dynamicObject;

        }


    }
}
