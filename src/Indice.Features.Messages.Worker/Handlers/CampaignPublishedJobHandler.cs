using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Worker.Handlers
{
    internal class CampaignPublishedJobHandler
    {
        public CampaignPublishedJobHandler(
            ILogger<CampaignPublishedJobHandler> logger,
            MessageJobHandlerFactory messageJobHandlerFactory
        ) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            MessageJobHandlerFactory = messageJobHandlerFactory;
        }

        public ILogger<CampaignPublishedJobHandler> Logger { get; }
        public MessageJobHandlerFactory MessageJobHandlerFactory { get; }

        public async Task Process(CampaignPublishedEvent campaign) {
            var handler = MessageJobHandlerFactory.Create<CampaignPublishedEvent>();
            await handler.Process(campaign);
        }
    }
}
