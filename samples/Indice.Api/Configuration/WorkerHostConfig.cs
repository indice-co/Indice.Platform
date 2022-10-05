using Indice.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
                jobsOptions.UseFilesAzure();
                jobsOptions.UsePushNotificationServiceAzure();
                jobsOptions.UseEmailServiceSparkpost(jobsOptions.Configuration);
                jobsOptions.UseSmsServiceYubotoOmni(jobsOptions.Configuration);
            });
    }
}
