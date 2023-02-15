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
                jobsOptions.QueuePollingInterval = 5000;
                jobsOptions.QueueMaxPollingInterval = 10000;
                jobsOptions.UseFilesAzure();
                jobsOptions.UsePushNotificationServiceAzure();
                jobsOptions.UseEmailServiceSparkpost(jobsOptions.Configuration);
                jobsOptions.UseSmsServiceYubotoOmni(jobsOptions.Configuration);
            });
    }
}
