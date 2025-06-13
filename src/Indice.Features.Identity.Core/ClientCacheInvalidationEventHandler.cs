#if NET9_0_OR_GREATER
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
#else
using IdentityServer4.Models;
using IdentityServer4.Services;
#endif
using Indice.Events;
using Indice.Features.Identity.Core.Events;

namespace Indice.Features.Identity.Core;
internal sealed class ClientCacheInvalidationEventHandler : IPlatformEventHandler<ClientUpdatedEvent>, IPlatformEventHandler<ClientDeletedEvent>
{
    public ClientCacheInvalidationEventHandler(ICache<Client> cache) {
        Cache = cache;
    }

    public ICache<Client> Cache { get; }

    public async Task Handle(ClientDeletedEvent @event, PlatformEventArgs args) {
        await Cache.SetAsync(@event.Client.ClientId, null!, TimeSpan.FromSeconds(1));
    }

    public async Task Handle(ClientUpdatedEvent @event, PlatformEventArgs args) {
        await Cache.SetAsync(@event.Client.ClientId, null!, TimeSpan.FromSeconds(1));
    }
}
