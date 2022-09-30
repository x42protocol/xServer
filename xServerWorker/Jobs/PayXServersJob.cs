using Common.Models.x42Blockcore;
using Common.Models.XServer;
using Quartz;
using RestSharp;
using System.Diagnostics;

namespace xServerWorker.Jobs
{
    [DisallowConcurrentExecution]
    public class PayXServersJob : IJob
    {
        public PayXServersJob()
        {
        }
        public async Task Execute(IJobExecutionContext context)
        {
            using (var loggerFactory = LoggerFactory.Create
          (
              builder =>
              {
                  builder.AddConsole();
              }
          ))
            {
                Console.Write("Pay XServer");

                var _logger = loggerFactory.CreateLogger<PayXServersJob>();

#if DEBUG
                var x42BlockCoreClient = new RestClient("http://localhost:42220/api/");
#else
 var x42BlockCoreClient = new RestClient("http://x42core:42220/api/");
#endif

                 
                var xServerClient = new RestClient("http://144.91.95.234:4242/");


                var request = new RestRequest("xServer/getxserverstats");
                var response = await x42BlockCoreClient.GetAsync<XServerStatsReponse>(request);


 
                if (response != null)
                {

                    var allNodes = response.Nodes;
                    var pingTaskList = new List<Task<PingResponse>>();

                    var continueTasks = new List<Task>();


                    foreach (var node in allNodes)
                    {

                        Console.WriteLine($"Node :  {node.Name}");


                        var pingRequest = new RestRequest("/ping");
                        var pingTask = xServerClient.GetAsync<PingResponse>(pingRequest);

                        var continueTask = pingTask.ContinueWith(async (response) =>
                         {
                             var pingResponse = await response;
                             if (pingResponse != null)
                             {
                                 Console.WriteLine();

                                 Console.WriteLine($"Version :  {pingResponse.Version}");
                                 Console.WriteLine($"BestBlockHeight :  {pingResponse.BestBlockHeight}");
                                 Console.WriteLine();

                             }

                         });

                        continueTasks.Add(continueTask);


                    }

                    var stopWatch = new Stopwatch();
                    stopWatch.Start();
                    Task.WaitAll(pingTaskList.ToArray());
                    Task.WaitAll(continueTasks.ToArray());
                    stopWatch.Stop();

                    Console.WriteLine($"Executed in L {stopWatch.ElapsedMilliseconds} ms");



                }

            }
        }
    }
}
