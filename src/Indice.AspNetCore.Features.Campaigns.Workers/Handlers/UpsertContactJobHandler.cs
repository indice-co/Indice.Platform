using Indice.AspNetCore.Features.Campaigns.Events;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class UpsertContactJobHandler : CampaignJobHandlerBase
    {
        public UpsertContactJobHandler(
            ILogger<UpsertContactJobHandler> logger,
            IServiceProvider serviceProvider
        ) : base(serviceProvider) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILogger<UpsertContactJobHandler> Logger { get; }

        public async Task Process(UpsertContactEvent @event) => await UpsertContact(@event);
    }
}
