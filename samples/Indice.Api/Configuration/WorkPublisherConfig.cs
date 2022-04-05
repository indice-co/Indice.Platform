using Indice.Features.Messages.Worker;
using Indice.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WorkPublisherConfig
    {
        public static WorkPublisherBuilder AddWorkPublisherConfig(this IServiceCollection services, IConfiguration configuration) =>
            services.AddWorkPublisher(options => {
                options.JsonOptions.JsonSerializerOptions.WriteIndented = true;
                options.UseStoreRelational(builder => builder.UseSqlServer(configuration.GetConnectionString("WorkerDb")));
            })
            .AddCampaignsEvents();
    }
}
