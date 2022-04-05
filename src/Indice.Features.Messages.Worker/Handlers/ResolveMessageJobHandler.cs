using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class ResolveMessageJobHandler
    {
        public ResolveMessageJobHandler(
            ILogger<ResolveMessageJobHandler> logger,
            CampaignJobHandlerFactory campaignJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CampaignJobHandlerFactory = campaignJobHandlerFactory;
        }

        public ILogger<ResolveMessageJobHandler> Logger { get; }
        public CampaignJobHandlerFactory CampaignJobHandlerFactory { get; }

        public async Task Process(ResolveMessageEvent @event) {
            var handler = CampaignJobHandlerFactory.Create<ResolveMessageEvent>();
            await handler.Process(@event);
        }
    }
}
