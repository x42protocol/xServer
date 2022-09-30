using Quartz;
using RestSharp;

namespace xServerWorker.Jobs
{
    [DisallowConcurrentExecution]
    public class BlockPullerJob : IJob
    {
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
                Console.Write("BlockPullerJob");

                var _logger = loggerFactory.CreateLogger<BlockPullerJob>();
#if DEBUG
        var _x42BlockCoreClient = new RestClient("http://localhost:42220/api/");
#else
        var _x42BlockCoreClient = new RestClient("http://x42core:42220/api/");
#endif


        var request = new RestRequest("BlockStore/getblockcount");
                var response = await _x42BlockCoreClient.GetAsync<int>(request);
              
            }
        }
    }
}
