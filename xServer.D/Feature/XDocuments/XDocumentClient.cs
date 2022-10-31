using Common.Models.Graviex;
using Common.Models.OrderBook;
using Common.Utils;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using x42.Feature.PowerDns;
using x42.Feature.X42Client;
using x42.Utilities;

namespace x42.Feature.XDocuments
{
    public class XDocumentClient
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _db;
        private readonly PowerDnsFeature _powerDnsService;
        private X42Node _x42Client;
        private X42ClientSettings _x42ClientSettings;

        /// <summary>Instance logger.</summary>

        /// <summary>
        ///     A cancellation token source that can cancel the node monitoring processes and is linked to the <see cref="IxServerLifetime.ApplicationStopping"/>.
        /// </summary>
        private CancellationTokenSource _networkCancellationTokenSource;

        /// <summary>Global application life cycle control - triggers when application shuts down.</summary>
        private readonly IxServerLifetime _serverLifetime;

        /// <summary>Factory for creating background async loop tasks.</summary>
        private readonly IAsyncLoopFactory _asyncLoopFactory;
        private readonly ILogger _logger;


        public XDocumentClient(PowerDnsFeature powerDnsService, X42ClientSettings x42ClientSettings, ILoggerFactory loggerFactory, IxServerLifetime serverLifetime, IAsyncLoopFactory asyncLoopFactory)
        {
            var conventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("camelCase", conventionPack, t => true);

            _logger = loggerFactory.CreateLogger(GetType().FullName);

            var mongoConnectionString = Environment.GetEnvironmentVariable("MONGOCONNECTIONSTRING");



#if DEBUG
            _client = new MongoClient($"mongodb://localhost:27017");
#else
            _client = new MongoClient(mongoConnectionString);
#endif

            _db = _client.GetDatabase("xServerDb");
            _powerDnsService = powerDnsService;
            _x42ClientSettings = x42ClientSettings;
            _serverLifetime = serverLifetime;
            _networkCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new[] { _serverLifetime.ApplicationStopping });
            _asyncLoopFactory = asyncLoopFactory;
            _x42Client = new X42Node(_x42ClientSettings.Name, _x42ClientSettings.Address, _x42ClientSettings.Port, _logger, _serverLifetime, _asyncLoopFactory, false);
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

        public async Task<object> GetDocumentByHash(string hash)
        {

            // Get xDocument Collection Reference
            var xDocumentHashCollection = _db.GetCollection<BsonDocument>("XDocumentHashReference");
            var filter = Builders<BsonDocument>.Filter.Eq("hash", hash);
            var document = xDocumentHashCollection.Find(filter).FirstOrDefault();
            var xDocumentCollection = _db.GetCollection<BsonDocument>("xDocument");
            var id = document["_id"].ToString();

            filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            document = xDocumentCollection.Find(filter).FirstOrDefault();

            var dynamicObject = JsonConvert.DeserializeObject<dynamic>(document.ToString());

            string serialized = Serialize(dynamicObject);

            // Convert request to JSON string

            return dynamicObject;

        }

        public Task<decimal> GetPriceLock(decimal value)
        {

            // Get xDocument Collection Reference
            var xDocumentDictionaryCollection = _db.GetCollection<BsonDocument>("Dictionary");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", "graviexOrderBook");
            var document = xDocumentDictionaryCollection.Find(filter).FirstOrDefault();
            var orderBook = JsonConvert.DeserializeObject<OrderBookModel>(document.ToString());
            var asks = orderBook.Asks.OrderBy(l => l.Price);
            var btcTickerFilter = Builders<BsonDocument>.Filter.Eq("_id", "btcTicker");
            var btcTickerDocument = xDocumentDictionaryCollection.Find(btcTickerFilter).FirstOrDefault();
            var graviexTicker = JsonConvert.DeserializeObject<GraviexTickerModel>(btcTickerDocument.ToString());
            var sellprice = graviexTicker.Ticker.Sell;

            decimal totalAmount = value / sellprice;
            decimal TotalQty = 0;


            foreach (var item in asks)
            {
                if ((item.Quantity * item.Price) < totalAmount)
                {
                    totalAmount = totalAmount - (item.Quantity * item.Price);
                    TotalQty += item.Quantity;
                    Console.WriteLine(item.Price + " : " + item.Quantity);
                }
                else
                {
                    TotalQty += totalAmount / item.Price;
                    break;
                }
            }

            var fee = TotalQty * 0.05m;

            return Task.FromResult(Math.Round(TotalQty - fee));

        }


        public async Task<string> AddActionRequest(object request, bool boadcast = false)
        {

            string jsonRequest = JsonConvert.SerializeObject(request);
            var dynamicObject = JsonConvert.DeserializeObject<dynamic>(jsonRequest);
            string data = JsonConvert.SerializeObject(dynamicObject["data"]);

            jsonRequest = JsonUtility.NormalizeJsonString(jsonRequest);

            if (dynamicObject["documentType"] != 1)
            {

                throw new NotImplementedException();

            }

            if (dynamicObject["keyAddress"] != null && dynamicObject["signature"] != null)
            {

                var dataObject1 = dynamicObject["data"];
                string dataObjectAsJson = Serialize(dataObject1);
                string key = dynamicObject["keyAddress"];
                string signature = dynamicObject["signature"];

                var isValid = await _x42Client.VerifyMessageAsync(key, dataObjectAsJson, signature);

                if (!isValid)
                {
                    throw new Exception("Invalid Signature");
                }
            }


            // Get xDocument Collection Reference
            var xDocument = _db.GetCollection<BsonDocument>("xDocumentPending");

            // Get xDocumentHashReference Collection Reference
            var xDocumentHashReference = _db.GetCollection<BsonDocument>("XDocumentHashReference");

            // Convert request to JSON string
            string json = Serialize(dynamicObject);

            // Calculate Document Hash
            var hash = HashString(json);
            var Id = Guid.NewGuid();


            dynamicObject._id = Id;
            json = Serialize(dynamicObject);


            BsonDocument xDocumentEntry
                = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);

            xDocument.InsertOne(xDocumentEntry);

            var xDocumentHashReferenceEntry = new BsonDocument
            {
                {"_id",  Id.ToString()  },
                {"hash",  hash.ToString()}
            };

            xDocumentHashReference.InsertOne(xDocumentHashReferenceEntry);

            if (boadcast) {

                var broadcastCollection = _db.GetCollection<BsonDocument>("BroadcastQueue");
                broadcastCollection.InsertOne(xDocumentEntry);

            }

            return hash;

        }

        public List<string> GetZonesByKeyAddress(string keyAddress)
        {

            var xDocumentDictionaryCollection = _db.GetCollection<BsonDocument>("DnsZones");
            var filter = Builders<BsonDocument>.Filter.Eq("keyAddress", keyAddress);
            var zones = xDocumentDictionaryCollection.Find(filter).ToList();
            var result = new List<string>();

            foreach (var zone in zones)
            {
                var entry = zone["zone"].ToString();
                result.Add(entry);
            }

            return result.ToList();

        }

        public bool ZoneExists(string zone)
        {

            var xDocumentDictionaryCollection = _db.GetCollection<BsonDocument>("zones");
            var filter = Builders<BsonDocument>.Filter.Eq("zone", zone);
            var zones = xDocumentDictionaryCollection.Find(filter).ToList();

            return zones.Count > 0;

        }

        private string Serialize(object obj)
        {
            string serialized = JsonConvert.SerializeObject(obj, Formatting.Indented);
            return JsonUtility.NormalizeJsonString(serialized);
        }

        private string HashString(string text, string salt = "")
        {
            if (String.IsNullOrEmpty(text))
            {
                return String.Empty;
            }

            // Uses SHA256 to create the hash
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                // Convert the string to a byte array first, to be processed
                byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(text + salt);
                byte[] hashBytes = sha.ComputeHash(textBytes);

                // Convert back to a string, removing the '-' that BitConverter adds
                string hash = BitConverter
                    .ToString(hashBytes)
                    .Replace("-", String.Empty);

                return hash;
            }
        }

        private static void ValidateModel(object app)
        {
            var context = new ValidationContext(app, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(app, context, validationResults, true);

            if (!isValid)
            {

                throw new Exception(validationResults.FirstOrDefault().ErrorMessage);

            }
        }

    }
}
