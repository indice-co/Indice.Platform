using System.Security.Claims;
#if NET9_0_OR_GREATER
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
#else
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
#endif
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Events.Models;
using Indice.Features.ActivityLogs.Events;
using Indice.Features.GeoIP;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Indice.Features.ActivityLogs.EventHandlers;

/// <summary>An event that is raised when a user is locked out.</summary>
public sealed class AccountLockedEventHandler : IPlatformEventHandler<AccountLockedEvent>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ExtendedSignInManager<User> _SignInManager;
    private readonly IClientStore _clientStore;
    private readonly IPAddressLocator _ipAddressLocator;
    private readonly IPlatformEventService _platformEvents;

    /// <summary>Creates a new instance of <see cref="AccountLockedEventHandler"/>.</summary>
    /// <param name="eventService">Interface for the event service.</param>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <param name="ActivityManager">The Activity manager used to facilitate the discovery of the current device.</param>
    /// <param name="clientStore">Retrieval of client configuration.</param>
    /// <param name="ipAddressLocator">The ip locator service</param>
    /// <param name="platformEvents">Platform event service</param>
    public AccountLockedEventHandler(
        IEventService eventService,
        IHttpContextAccessor httpContextAccessor,
        ExtendedSignInManager<User> SignInManager,
        IClientStore clientStore,
        IPAddressLocator ipAddressLocator,
        IPlatformEventService platformEvents) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _SignInManager = SignInManager ?? throw new ArgumentNullException(nameof(SignInManager));
        _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        _ipAddressLocator = ipAddressLocator ?? throw new ArgumentNullException(nameof(ipAddressLocator));
        _platformEvents = platformEvents ?? throw new ArgumentNullException(nameof(platformEvents));
    }

    /// <inheritdoc />
    public async Task Handle(AccountLockedEvent @event, PlatformEventArgs args) {

    }
}