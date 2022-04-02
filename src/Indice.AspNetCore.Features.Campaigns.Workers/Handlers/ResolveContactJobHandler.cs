using Indice.AspNetCore.Features.Campaigns.Events;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class ResolveContactJobHandler : CampaignJobHandlerBase
    {
        public ResolveContactJobHandler(
            ILogger<ResolveContactJobHandler> logger,
            IServiceProvider serviceProvider
        ) : base(serviceProvider) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILogger<ResolveContactJobHandler> Logger { get; }

        public async Task Process(ContactResolutionEvent @event) => await ResolveContact(@event);
    }
}
