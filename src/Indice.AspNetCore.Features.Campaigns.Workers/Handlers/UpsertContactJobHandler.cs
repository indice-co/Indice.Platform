using Indice.AspNetCore.Features.Campaigns.Events;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class UpsertContactJobHandler
    {
        public UpsertContactJobHandler(
            ILogger<UpsertContactJobHandler> logger,
            CampaignJobHandlerFactory campaignJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CampaignJobHandlerFactory = campaignJobHandlerFactory ?? throw new ArgumentNullException(nameof(campaignJobHandlerFactory));
        }

        public ILogger<UpsertContactJobHandler> Logger { get; }
        public CampaignJobHandlerFactory CampaignJobHandlerFactory { get; }

        public async Task Process(UpsertContactEvent @event) {
            var handler = CampaignJobHandlerFactory.Create<UpsertContactEvent>();
            await handler.Process(@event);
        }
    }
}
