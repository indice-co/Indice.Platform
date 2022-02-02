using System;
using System.Threading.Tasks;
using Indice.Hosting.Tasks;
using Indice.Services;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Hosting
{
    internal class CampaignCreatedEventHandler : IPlatformEventHandler<CampaignCreatedEvent>
    {
        public CampaignCreatedEventHandler(ILogger<CampaignCreatedEventHandler> logger, IMessageQueue<CampaignQueueItem> messageQueue) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public ILogger<CampaignCreatedEventHandler> Logger { get; }
        public IMessageQueue<CampaignQueueItem> MessageQueue { get; }

        public async Task Handle(CampaignCreatedEvent @event) => await MessageQueue.Enqueue(@event.Campaign);
    }
}
