using Indice.AspNetCore.Features.Campaigns.Events;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class InboxDistributionJobHandler
    {
        public InboxDistributionJobHandler(
            ILogger<InboxDistributionJobHandler> logger,
            CampaignJobHandlerFactory campaignJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CampaignJobHandlerFactory = campaignJobHandlerFactory ?? throw new ArgumentNullException(nameof(campaignJobHandlerFactory));
        }

        public ILogger<InboxDistributionJobHandler> Logger { get; }
        public CampaignJobHandlerFactory CampaignJobHandlerFactory { get; }

        public async Task Process(InboxDistributionEvent inboxDistribution) {
            var handler = CampaignJobHandlerFactory.Create<InboxDistributionEvent>();
            await handler.Process(inboxDistribution);
        }
    }
}
