using Indice.Api.JobHandlers;
using Indice.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WorkerHostConfig
    {
        public static WorkerHostBuilder AddWorkerHostConfig(this IServiceCollection services, IConfiguration configuration) =>
            services.AddWorkerHost(hostOptions => {
                hostOptions.JsonOptions.JsonSerializerOptions.WriteIndented = true;
                hostOptions.UseStoreRelational(builder => builder.UseSqlServer(configuration.GetConnectionString("WorkerDb")));
                hostOptions.UseLockManagerAzure();
            })
            .AddMessageJobs(jobsOptions => {
                jobsOptions.QueuePollingInterval = 300;
                jobsOptions.QueueMaxPollingInterval = 5000;
                //jobsOptions.UseFilesAzure();
                jobsOptions.UseFilesLocal(fileOptions => fileOptions.Path = "uploads");
                jobsOptions.UsePushNotificationServiceAzure();
                jobsOptions.UseEmailServiceSparkpost(jobsOptions.Configuration);
                jobsOptions.UseSmsServiceYubotoOmni(jobsOptions.Configuration);
            })
            .AddJob<ExtractWebsitesJobHandler>().WithQueueTrigger<ExtractWebsitesCommand>(options => options.QueueName = "extract-websites")
            .AddJob<DownloadWebsiteContentJobHandler>().WithQueueTrigger<DownloadWebsiteCommand>(options => options.QueueName = "download-website-content");
    }
}
