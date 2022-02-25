using System;
using System.Threading.Tasks;
using Indice.Events;
using Indice.Services;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns
{
    internal class CampaignCreatedEventHandler : IPlatformEventHandler<CampaignCreatedEvent>
    {
        public CampaignCreatedEventHandler(ILogger<CampaignCreatedEventHandler> logger, Func<string, IEventDispatcher> getEventDispatcher) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EventDispatcher = getEventDispatcher(CampaignsApi.EventDispatcherAzureServiceKey) ?? throw new ArgumentNullException(nameof(getEventDispatcher));
        }

        public ILogger<CampaignCreatedEventHandler> Logger { get; }
        public IEventDispatcher EventDispatcher { get; }

        public async Task Handle(CampaignCreatedEvent @event) => await EventDispatcher.RaiseEventAsync(@event.Campaign, options => options.WrapInEnvelope(false).WithQueueName(QueueNames.CampaignCreated));
    }
}
