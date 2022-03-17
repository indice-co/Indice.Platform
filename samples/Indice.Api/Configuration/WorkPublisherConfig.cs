using Indice.AspNetCore.Features.Campaigns;
using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WorkPublisherConfig
    {
        public static WorkPublisherBuilder AddWorkPublisherConfig(this IServiceCollection services, IConfiguration configuration) {
            var workPublisherBuilder = services.AddWorkPublisher(options => {
                options.JsonOptions.JsonSerializerOptions.WriteIndented = true;
                options.UseStoreRelational(builder => builder.UseSqlServer(configuration.GetConnectionString("WorkerDb")));
            })
            .ForEvent<CampaignCreatedEvent>(QueueNames.CampaignCreated)
            .ForEvent<SendPushNotificationEvent>(QueueNames.SendPushNotification);
            return workPublisherBuilder;
        }
    }
}
