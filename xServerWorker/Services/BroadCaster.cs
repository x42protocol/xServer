using Common.Models.x42Blockcore;
using Common.Models.XServer;
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


        public BroadCaster(ILogger<BroadCaster> logger)
        {
            _logger = logger;
         }


        public async Task<bool> BroadcastXDocument() {

         
 

        var request = new RestRequest("xServer/getxserverstats");
            var response = await _x42BlockCoreClient.GetAsync<XServerStatsReponse>(request);

            if (response != null)
            {

                var allNodes = response.Nodes;
                var pingTaskList = new List<Task<PingResponse>>();

                var continueTasks = new List<Task>();


                foreach (var node in allNodes)
                {

                    try
                    {
                        string xServerURL = GetServerUrl(node.NetworkProtocol, node.NetworkAddress, node.NetworkPort);
                        var xServerClient = new RestClient(xServerURL);

                        var pingRequest = new RestRequest("/ping");
                        var pingTask = xServerClient.GetAsync<PingResponse>(pingRequest);
                        pingTaskList.Add(pingTask);

                        var continueTask = pingTask.ContinueWith(async (response) =>
                        {
                            var pingResponse = await response;

 
                            if (pingResponse != null)
                            {

 
                            }

                            else
                            {

                            }

                        });

                        continueTasks.Add(continueTask);
                    }
                    catch (Exception e)
                    {

                        _logger.LogError(e.Message);
                    }



                }

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                Task.WaitAll(pingTaskList.ToArray());
                Task.WaitAll(continueTasks.ToArray());
                stopWatch.Stop();

                Console.WriteLine($"Executed in L {stopWatch.ElapsedMilliseconds} ms");


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
