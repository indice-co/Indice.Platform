using IdentityServer4.Services;
using Indice.Features.Identity.Core.Events;
using Indice.Services;

namespace Indice.Features.Identity.Core;

internal class UserBlockedEventHandler : IPlatformEventHandler<UserBlockedEvent>
{
    private readonly IPersistedGrantService _persistedGrantService;

    public UserBlockedEventHandler(IPersistedGrantService persistedGrantService) => _persistedGrantService = persistedGrantService ?? throw new ArgumentNullException(nameof(persistedGrantService));

    public async Task Handle(UserBlockedEvent @event, PlatformEventArgs args) => await _persistedGrantService.RemoveAllGrantsAsync(@event.User.Id);
}
