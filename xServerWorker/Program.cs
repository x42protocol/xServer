using Common.Services;
using xServerWorker;
using xServerWorker.BackgroundServices;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<BlockProcessingWorker>();
        try
        {
            services.AddSingleton<PowerDnsRestClient, PowerDnsRestClient>();
            services.AddSingleton<IDappProvisioner, DappProvisioner>();
            
        }
        catch (Exception)
        {

            throw;
        }

    })
    .Build();

await QuartzJobConfigurator.ConfigureJobsAsync();

await host.RunAsync();
