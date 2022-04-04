using Indice.AspNetCore.Features.Campaigns.Events;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class PersistInboxMessageJobHandler
    {
        public PersistInboxMessageJobHandler(
            ILogger<PersistInboxMessageJobHandler> logger,
            CampaignJobHandlerFactory campaignJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CampaignJobHandlerFactory = campaignJobHandlerFactory ?? throw new ArgumentNullException(nameof(campaignJobHandlerFactory));
        }

        public ILogger<PersistInboxMessageJobHandler> Logger { get; }
        public CampaignJobHandlerFactory CampaignJobHandlerFactory { get; }

        public async Task Process(PersistInboxMessageEvent message) {
            var handler = CampaignJobHandlerFactory.Create<PersistInboxMessageEvent>();
            await handler.Process(message);
        }
    }
}
