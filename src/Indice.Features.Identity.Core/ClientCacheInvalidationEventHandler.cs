using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
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
        await Cache.RemoveAsync(@event.Client.ClientId);
    }

    public async Task Handle(ClientUpdatedEvent @event, PlatformEventArgs args) {
        await Cache.RemoveAsync(@event.Client.ClientId);
    }
}
