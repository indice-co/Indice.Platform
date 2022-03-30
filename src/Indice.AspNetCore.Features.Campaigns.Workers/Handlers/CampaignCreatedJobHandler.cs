using Indice.AspNetCore.Features.Campaigns.Events;
using Indice.Services;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    /// <summary>
    /// This job handler executes when a new campaign is created. It checks for campaign's delivery channel and distributes work accordingly to the next hop.
    /// </summary>
    internal class CampaignCreatedJobHandler : CampaignJobHandlerBase
    {
        public CampaignCreatedJobHandler(
            ILogger<CampaignCreatedJobHandler> logger,
            Func<string, IEventDispatcher> getEventDispatcher,
            Func<string, IPushNotificationService> getPushNotificationService
        ) : base(getEventDispatcher, getPushNotificationService) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILogger<CampaignCreatedJobHandler> Logger { get; }

        public async Task Process(CampaignCreatedEvent campaign) => await TryDistributeCampaign(campaign);
    }
}
