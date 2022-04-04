using Indice.AspNetCore.Features.Campaigns.Events;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class CampaignCreatedJobHandler : CampaignJobHandlerBase
    {
        public CampaignCreatedJobHandler(
            ILogger<CampaignCreatedJobHandler> logger,
            IServiceProvider serviceProvider
        ) : base(serviceProvider) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILogger<CampaignCreatedJobHandler> Logger { get; }

        public async Task Process(CampaignCreatedEvent campaign) => await TryDistribute(campaign);
    }
}
