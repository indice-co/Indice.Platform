using Indice.AspNetCore.Features.Campaigns.Events;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class ContactResolutionJobHandler
    {
        public ContactResolutionJobHandler(
            ILogger<ContactResolutionJobHandler> logger,
            CampaignJobHandlerFactory campaignJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CampaignJobHandlerFactory = campaignJobHandlerFactory;
        }

        public ILogger<ContactResolutionJobHandler> Logger { get; }
        public CampaignJobHandlerFactory CampaignJobHandlerFactory { get; }

        public async Task Process(ContactResolutionEvent @event) {
            var handler = CampaignJobHandlerFactory.Create<ContactResolutionEvent>();
            await handler.Process(@event);
        }
    }
}
