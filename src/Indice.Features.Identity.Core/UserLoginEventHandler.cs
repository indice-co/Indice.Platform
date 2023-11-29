using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.Features.Identity.Core.Events;
using Indice.Services;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Identity.Core;

/// <summary>An event that is raised when a user is fully logged in into the identity system.</summary>
public sealed class UserLoginEventHandler : IPlatformEventHandler<UserLoginEvent>
{
    private readonly IEventService _eventService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IClientStore _clientStore;

    /// <summary>Creates a new instance of <see cref="UserLoginEventHandler"/>.</summary>
    /// <param name="eventService">Interface for the event service.</param>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <param name="clientStore">Retrieval of client configuration.</param>
    public UserLoginEventHandler(
        IEventService eventService,
        IHttpContextAccessor httpContextAccessor,
        IClientStore clientStore
    ) {
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _httpContextAccessor = httpContextAccessor;
        _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
    }

    /// <inheritdoc />
    public async Task Handle(UserLoginEvent @event, PlatformEventArgs args) {
        if (@event.Succeeded) {
            var clientId = _httpContextAccessor?.HttpContext?.GetClientIdFromReturnUrl();
            Client client = null;
            if (!string.IsNullOrWhiteSpace(clientId)) {
                client = await _clientStore.FindClientByIdAsync(clientId);
            }
            await _eventService.RaiseAsync(new ExtendedUserLoginSuccessEvent(
                @event.User.UserName, 
                @event.User.Id, 
                @event.User.UserName, 
                clientId: clientId, 
                clientName: client?.ClientName, 
                warning: @event.Warning, 
                authenticationMethods: @event.AuthenticationMethods
            ));
        }
    }
}