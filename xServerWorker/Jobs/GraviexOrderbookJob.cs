using Common.Models.Graviex;
using Common.Models.OrderBook;
using Common.Utils;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Newtonsoft.Json;
using Quartz;
using RestSharp;
using System.Dynamic;

namespace xServerWorker.Jobs
{
    [DisallowConcurrentExecution]
    public class GraviexOrderbookJob : IJob
    {
        private readonly MongoClient _client;
        private readonly IMongoDatabase _db;

        public GraviexOrderbookJob()
        {
#if DEBUG
            _client = new MongoClient($"mongodb://localhost:27017");
#else

            var mongoConnectionString = Environment.GetEnvironmentVariable("MONGOCONNECTIONSTRING");


            _client = new MongoClient(mongoConnectionString);
#endif

            _db = _client.GetDatabase("xServerDb");

        }
        public async Task Execute(IJobExecutionContext context)
        {
            using (var loggerFactory = LoggerFactory.Create
           (
               builder =>
               {
                   builder.AddConsole();
               }
           )) ;
            var client = new RestClient("https://graviex.net/api/v2/");

            var request = new RestRequest("depth.json?market=x42btc&limit=50");
            var response = await client.GetAsync<GraviexOrderBookModel>(request);

            var orderBookModel = new OrderBookModel(response.Timestamp);


            orderBookModel.Asks = response.Asks.Select(item => new OrderModel(item[0], item[1])).ToList();
            orderBookModel.Bids = response.Bids.Select(item => new OrderModel(item[0], item[1])).ToList();

            var dictionaryCollaection = _db.GetCollection<BsonDocument>("Dictionary");

            var filter = Builders<BsonDocument>.Filter.Eq("_id", "graviexOrderBook");
            var document = dictionaryCollaection.Find(filter).FirstOrDefault();

            dynamic orderBook = new ExpandoObject();
            orderBook._id = "graviexOrderBook";
            orderBook.timestamp = orderBookModel.Timestamp;
            orderBook.asks = orderBookModel.Asks;
            orderBook.bids = orderBookModel.Bids;


            var jsonstring = JsonConvert.SerializeObject(orderBook);

            jsonstring = JsonUtility.NormalizeJsonString(jsonstring);

            orderBook = JsonConvert.DeserializeObject<dynamic>(jsonstring);

            var updateDocument = BsonSerializer.Deserialize<BsonDocument>(jsonstring);

            var camelCaseConventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
            ConventionRegistry.Register("CamelCase", camelCaseConventionPack, type => true);

            if (document != null)
            {

                var builder = Builders<BsonDocument>.Filter;

                filter = builder.Eq("_id", "graviexOrderBook");
                var update = Builders<BsonDocument>.Update
                    .Set("timestamp", orderBookModel.Timestamp)
                    .Set("bids", orderBookModel.Bids)
                    .Set("asks", orderBookModel.Asks);
                dictionaryCollaection.UpdateOne(filter, update);



            }
            else
            {

                dictionaryCollaection.InsertOne(updateDocument);
                document = updateDocument;

            }

            filter = Builders<BsonDocument>.Filter.Eq("_id", "btcTicker");
            document = dictionaryCollaection.Find(filter).FirstOrDefault();



            var tickerRequest = new RestRequest("tickers/btcusdt.json");
            var tickerReponse = await client.GetAsync<GraviexTickerModel>(tickerRequest);

            jsonstring = JsonConvert.SerializeObject(tickerReponse);
            jsonstring = JsonUtility.NormalizeJsonString(jsonstring);
            tickerReponse = JsonConvert.DeserializeObject<GraviexTickerModel>(jsonstring);

            if (document != null)
            {

                var builder = Builders<BsonDocument>.Filter;

                filter = builder.Eq("_id", "btcTicker");
                var update = Builders<BsonDocument>.Update
                    .Set("at", tickerReponse.At)
                    .Set("ticker", tickerReponse.Ticker);
                dictionaryCollaection.UpdateOne(filter, update);



            }
            else
            {
                dynamic ticker = new ExpandoObject();
                ticker._id = "btcTicker";
                ticker.at = tickerReponse.At;
                ticker.ticker = tickerReponse.Ticker;

                jsonstring = JsonConvert.SerializeObject(ticker);
                jsonstring = JsonUtility.NormalizeJsonString(jsonstring);


                updateDocument = BsonSerializer.Deserialize<BsonDocument>(jsonstring);

                dictionaryCollaection.InsertOne(updateDocument);

            }









        }
    }
}
