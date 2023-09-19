using IdentityServer4.Services;
using Indice.Features.Identity.Core.Events;
using Indice.Services;

namespace Indice.Features.Identity.UI;

internal class UserLoginEventHandler : IPlatformEventHandler<UserLoginEvent>
{
    private readonly IEventService _eventService;

    /// <summary>Creates a new instance of <see cref="UserLoginEventHandler"/>.</summary>
    /// <param name="eventService">Interface for the event service.</param>
    public UserLoginEventHandler(IEventService eventService) {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
    }

    public async Task Handle(UserLoginEvent @event, PlatformEventArgs args) {
        if (@event.Succeeded) {
            await _eventService.RaiseAsync(new ExtendedUserLoginSuccessEvent(@event.User.UserName, @event.User.Id, @event.User.UserName, warning: @event.Warning));
        }
    }
}
