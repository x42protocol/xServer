using Quartz;
using Quartz.Impl;
using xServerWorker.Jobs;

namespace xServerWorker
{
    public static class QuartzJobConfigurator
    {
        private static StdSchedulerFactory factory = new StdSchedulerFactory();

        public static async Task ConfigureJobsAsync()
        {

            IScheduler scheduler = await factory.GetScheduler();
            await scheduler.Start();

            await AddJobToScheduler<GraviexOrderbookJob>(scheduler, "0/10 0/1 * 1/1 * ? *");
          //  await AddJobToScheduler<PayXServersJob>(scheduler, "0/10 0/1 * 1/1 * ? *");

        }


        private static async Task AddJobToScheduler<T>(IScheduler scheduler, string cronExpression) where T : IJob
        {
            IJobDetail job = JobBuilder.Create<T>()
                .Build();


            ITrigger trigger = TriggerBuilder.Create()
                .WithCronSchedule(cronExpression)
                .ForJob(job)
                .Build();

            // Tell quartz to schedule the job using our trigger
            await scheduler.ScheduleJob(job, trigger);
        }


    }
}
