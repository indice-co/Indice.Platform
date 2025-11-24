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
using Indice.Features.Identity.SignInLogs.Events;
using Indice.Features.GeoIP;
using Indice.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Indice.Features.Identity.SignInLogs.EventHandlers;

/// <summary>An event that is raised when a user successfully changes their password.</summary>
public sealed class UserPasswordChangedEventHandler : IPlatformEventHandler<PasswordChangedEvent>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ExtendedSignInManager<User> _signInManager;
    private readonly IClientStore _clientStore;
    private readonly IPAddressLocator _ipAddressLocator;
    private readonly IPlatformEventService _platformEvents;

    /// <summary>Creates a new instance of <see cref="UserPasswordChangedEventHandler"/>.</summary>
    /// <param name="eventService">Interface for the event service.</param>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>, if one is available.</param>
    /// <param name="signInManager">The signin manager used to facilitate the discovery of the current device.</param>
    /// <param name="clientStore">Retrieval of client configuration.</param>
    /// <param name="ipAddressLocator">The ip locator service</param>
    /// <param name="platformEvents">Platform event service</param>
    public UserPasswordChangedEventHandler(
        IEventService eventService,
        IHttpContextAccessor httpContextAccessor,
        ExtendedSignInManager<User> signInManager,
        IClientStore clientStore,
        IPAddressLocator ipAddressLocator,
        IPlatformEventService platformEvents) {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        _ipAddressLocator = ipAddressLocator ?? throw new ArgumentNullException(nameof(ipAddressLocator));
        _platformEvents = platformEvents ?? throw new ArgumentNullException(nameof(platformEvents));
    }

    /// <inheritdoc />
    public async Task Handle(PasswordChangedEvent @event, PlatformEventArgs args) {
        var clientId = _httpContextAccessor?.HttpContext?.GetClientIdFromReturnUrl() ?? _httpContextAccessor?.HttpContext?.User.FindFirstValue(BasicClaimTypes.ClientId);
        var userManager = (ExtendedUserManager<User>)_signInManager.UserManager;
        var user = await _signInManager.UserManager.FindByIdAsync(@event.User.Id);
        var deviceId = await _signInManager.GetMfaDeviceIdentifierAsync(user!);
        var ipLocation = _ipAddressLocator.GetLocationMetadata(_httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress!);
        var claims = await userManager.GetClaimsAsync(user!);
        var userLocale = claims.FirstOrDefault(c => c.Type == BasicClaimTypes.Locale)?.Value;

        UserDevice? device = null;
        if (!deviceId.IsEmpty) {
            // If the device id is available populate data.
            device = await userManager.GetDeviceByIdAsync(user!, deviceId.Value!);
        }
        if (device is null) {
            var userAgentHeader = _httpContextAccessor?.HttpContext?.Request.Headers[HeaderNames.UserAgent];
            if (!string.IsNullOrWhiteSpace(userAgentHeader)) {
                device = UserDevice.FromUserAgent(userAgentHeader!, deviceId, user.Id, 0);
            }
        }
        Client? client = null;
        if (!string.IsNullOrWhiteSpace(clientId)) {
            client = await _clientStore.FindClientByIdAsync(clientId);
        }
        await _platformEvents.Publish(new SecurityNotificationEvent(nameof(PasswordChangedEvent), UserEventContext.InitializeFromUser(user!), ipLocation) {
            Device = device is not null ? UserDeviceEventContext.InitializeFromUserDevice(device) : null,
            Client = client is not null ? ClientEventContext.InitializeFromClient(client) : null,
            TimeStamp = DateTimeOffset.UtcNow,
            Locale = userLocale
        });
    }
}