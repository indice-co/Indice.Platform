using Indice.AspNetCore.Features.Campaigns.Events;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Features.Campaigns.Workers
{
    internal class PersistInboxMessageJobHandler : CampaignJobHandlerBase
    {
        public PersistInboxMessageJobHandler(
            ILogger<PersistInboxMessageJobHandler> logger,
            IServiceProvider serviceProvider
        ) : base(serviceProvider) {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ILogger<PersistInboxMessageJobHandler> Logger { get; }

        public async Task Process(PersistInboxMessageEvent message) => await PersistInboxMessage(message);
    }
}
