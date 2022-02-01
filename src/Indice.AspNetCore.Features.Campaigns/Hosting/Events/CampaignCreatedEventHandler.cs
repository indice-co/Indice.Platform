using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Hosting.Tasks;
using Indice.Services;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Hosting
{
    internal class CampaignCreatedEventHandler : IPlatformEventHandler<CampaignCreatedEvent>
    {
        public CampaignCreatedEventHandler(ILogger<CampaignCreatedEventHandler> logger, IMessageQueue<Campaign> messageQueue) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            MessageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        public ILogger<CampaignCreatedEventHandler> Logger { get; }
        public IMessageQueue<Campaign> MessageQueue { get; }

        public async Task Handle(CampaignCreatedEvent @event) {
            await MessageQueue.Enqueue(new Campaign { 
                Id = @event.Campaign.Id,
                ActionText = @event.Campaign.ActionText,
                ActionUrl = @event.Campaign.ActionUrl,
                ActivePeriod = @event.Campaign.ActivePeriod,
                Content = @event.Campaign.Content,
                CreatedAt = @event.Campaign.CreatedAt,
                Data = @event.Campaign.Data,
                DeliveryChannel = @event.Campaign.DeliveryChannel,
                IsGlobal = @event.Campaign.IsGlobal,
                Published = @event.Campaign.Published,
                Title = @event.Campaign.Title,
                Type = @event.Campaign.Type
            });
        }
    }
}
