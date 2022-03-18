// See https://aka.ms/new-console-template for more information
using Indice.Hosting;
using Indice.WorkerHost.JobHandlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder();
// Configure the host.
host.ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) => {
    configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
})
.ConfigureServices((hostBuilderContext, services) => {
    services.Configure<LoggerFilterOptions>(loggerFilterOptions => loggerFilterOptions.MinLevel = LogLevel.Information)
            .AddWorkerHost(options => {
                options.JsonOptions.JsonSerializerOptions.WriteIndented = true;
                options.UseStoreRelational(builder => builder.UseSqlServer(hostBuilderContext.Configuration.GetConnectionString("WorkerDb")));
            })
            //.AddAlertJobs()
            .AddCampaignsJobs(options => {
                //options.UsePushNotificationServiceAzure();
            });
})
.ConfigureLogging(loggingBuilder => loggingBuilder.AddConsole())
.UseEnvironment("Development");
// Build host and run.
await host.Build().RunAsync();

public static class WorkerHostBuilderExtensions
{
    public static WorkerHostBuilder AddAlertJobs(this WorkerHostBuilder workerHostBuilder) => workerHostBuilder
        .AddJob<LoadAvailableAlertsJobHandler>()
        .WithScheduleTrigger<DemoCounterDto>("0 0/1 * * * ?", scheduleOptions => {
            scheduleOptions.Name = "load-available-alerts";
            scheduleOptions.Description = "Load alerts for the queue.";
            scheduleOptions.Group = "indice";
        })
        .AddJob<SendSmsJobHandler>()
        .WithQueueTrigger<SmsDto>(queueOptions => {
            queueOptions.QueueName = "send-user-sms";
            queueOptions.PollingInterval = 10000;
            queueOptions.InstanceCount = 1;
        });
}
