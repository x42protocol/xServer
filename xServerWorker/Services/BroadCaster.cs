using Common.Models.x42Blockcore;
using Common.Models.XServer;
using MongoDB.Bson;
using MongoDB.Driver;
using RestSharp;
using System.Diagnostics;

namespace xServerWorker.Services
{
    public  class BroadCaster
    {
        private readonly ILogger<BroadCaster> _logger;

#if DEBUG
        private readonly RestClient _x42BlockCoreClient = new RestClient("http://localhost:42220/api/");
#else
        private readonly RestClient _x42BlockCoreClient = new RestClient("http://x42core:42220/api/");
#endif
        private readonly IMongoDatabase _db;
        private readonly IMongoClient _client;


        public BroadCaster(ILogger<BroadCaster> logger, IMongoDatabase db, IMongoClient client)
        {
            _logger = logger;
            _client = new MongoClient($"mongodb://localhost:27017");
            _db = _client.GetDatabase("xServerDb");

        }


        public async Task<bool> BroadcastXDocument() {


            var broadcastCollection = _db.GetCollection<BsonDocument>("BroadcastQueue");
            var document = broadcastCollection.Find("").FirstOrDefault();

            if (document == null) {

                return false;
            }


            var request = new RestRequest("xServer/getxserverstats");
            var response = await _x42BlockCoreClient.GetAsync<XServerStatsReponse>(request);


            var serverlist = new List<string>();
            serverlist.Add("Eleni");
            serverlist.Add("Alexandros"); 
            serverlist.Add("dimiT3");
            if (response != null)
            {

                var allNodes = response.Nodes.Where(l => serverlist.Contains(l.Name));
                var broadcastTaskList = new List<Task>();



                foreach (var node in allNodes)
                {

                    try
                    {
                        string xServerURL = GetServerUrl(node.NetworkProtocol, node.NetworkAddress, node.NetworkPort);
                        var xServerClient = new RestClient(xServerURL);

                        var broadcastRequest = new RestRequest("/xDocument/");
                        var pingTask = xServerClient.PostAsync(broadcastRequest);
                        
                        broadcastTaskList.Add(pingTask);
                    

                     }
                    catch (Exception e)
                    {

                        _logger.LogError(e.Message);
                    }

                }

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                Task.WaitAll(broadcastTaskList.ToArray());
                stopWatch.Stop();

                Console.WriteLine($"Document broadcast in  {stopWatch.ElapsedMilliseconds} ms");


            }

                return true;


         }
        public string GetServerUrl(int networkProtocol, string networkAddress, long networkPort)
        {
            return $"{GetProtocolString(networkProtocol)}://{networkAddress}:{networkPort}/";
        }

        private string GetProtocolString(int networkProtocol)
        {
            string result = string.Empty;
            switch (networkProtocol)
            {
                case 0: // Default to HTTP
                case 1:
                    result = "http";
                    break;
                case 2:
                    result = "https";
                    break;
                case 3:
                    // TODO: Add Websocket
                    result = "ws";
                    break;
            }
            return result;
        }
    }
}
