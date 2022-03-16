using Indice.Api.JobHandlers;
using Indice.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WorkerHostConfig
    {
        public static WorkerHostBuilder AddWorkerHostConfig(this IServiceCollection services, IConfiguration configuration) {
            var workerHostBuilder = services.AddWorkerHost(options => {
                options.JsonOptions.JsonSerializerOptions.WriteIndented = true;
                options.UseStoreRelational(builder => builder.UseSqlServer(configuration.GetConnectionString("WorkerDb")));
                //options.UseStoreRelational<ExtendedTaskDbContext>(builder => builder.UseNpgsql(Configuration.GetConnectionString("WorkerDb")));
            })
            .AddCampaignsJobs(options => {
                //options.UsePushNotificationServiceAzure();
            })
            /*.AddSampleJobs()*/;
            return workerHostBuilder;
        }

        private static WorkerHostBuilder AddSampleJobs(this WorkerHostBuilder workerHostBuilder) {
            workerHostBuilder.AddJob<LongRunningTaskJobHandler>()
                             .WithScheduleTrigger("0 0/2 * * * ?", options => {
                                 options.Name = "useless-task";
                                 options.Description = "Does nothing for some minutes.";
                                 options.Group = "indice";
                                 options.Singleton = true;
                             })
                             .AddJob<LoadAvailableAlertsJobHandler>()
                             .WithScheduleTrigger<DemoCounterModel>("0 0/1 * * * ?", options => {
                                 options.Name = "load-available-alerts";
                                 options.Description = "Load alerts for the queue.";
                                 options.Group = "indice";
                             })
                             .AddJob<SendSmsJobHandler>()
                             .WithQueueTrigger<SmsDto>(options => {
                                 options.QueueName = "send-user-sms";
                                 options.PollingInterval = 500;
                                 options.InstanceCount = 3;
                             })
                             .AddJob<LogSendSmsJobHandler>()
                             .WithQueueTrigger<LogSmsDto>(options => {
                                 options.QueueName = "log-send-user-sms";
                                 options.PollingInterval = 500;
                                 options.InstanceCount = 1;
                             });
            return workerHostBuilder;
        }
    }
}
