using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Hubs;
using Indice.Features.Identity.Core.Models;
using Indice.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core;

/// <summary>Default implementation of <see cref="IAuthenticationMethodProvider"/> where authentication methods are statically configured.</summary>
public class AuthenticationMethodProviderInMemory : IAuthenticationMethodProvider
{
    private readonly AuthenticationMethod[] _authenticationMethods;
    private readonly IConfiguration _configuration;
    private readonly ExtendedUserManager<User> _userManager;

    /// <summary>Creates a new instance of <see cref="AuthenticationMethodProviderInMemory"/>.</summary>
    /// <param name="authenticationMethods">The authentication methods to apply in the identity system.</param>
    /// <param name="multiFactorAuthenticationHubs"></param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AuthenticationMethodProviderInMemory(
        IEnumerable<AuthenticationMethod> authenticationMethods,
        IEnumerable<IHubContext<MultiFactorAuthenticationHub>> multiFactorAuthenticationHubs,
        IConfiguration configuration,
        ExtendedUserManager<User> userManager
    ) {
        _authenticationMethods = authenticationMethods?.OrderByDescending(x => x.SecurityLevel).ToArray() ?? throw new ArgumentNullException(nameof(authenticationMethods));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        HubContext = multiFactorAuthenticationHubs?.FirstOrDefault();
        AllowMfaChannelDowngrade = _configuration.GetIdentityOption<bool>($"{nameof(IdentityOptions.SignIn)}:Mfa", "AllowDowngradeAuthenticationMethod");
    }

    /// <inheritdoc />
    public IHubContext<MultiFactorAuthenticationHub>? HubContext { get; }

    /// <inheritdoc />
    public bool AllowMfaChannelDowngrade { get; }

    /// <inheritdoc />
    public Task<AuthenticationMethod[]> GetAllMethodsAsync() => Task.FromResult(_authenticationMethods);

    /// <inheritdoc />
    /// <remarks>For now the supported authentication methods are <see cref="SmsAuthenticationMethod"/>, <see cref="TrustedDeviceAuthenticationMethod"/> and <see cref="AuthenticatorAppAuthenticationMethod"/>.</remarks>
    public async Task<AuthenticationMethod?> FindMethodForUserOrDefaultAsync(User user, TotpDeliveryChannel? channel = null) {
        var userMethods = await GetAllMethodsForUserAsync(user);
        if (channel.HasValue && AllowMfaChannelDowngrade) {
            return userMethods.FirstOrDefault(x => x.GetDeliveryChannel() == channel.Value) ?? userMethods.FirstOrDefault();
        }
        return userMethods.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<AuthenticationMethod[]> GetAllMethodsForUserAsync(User user) {
        var methods = new List<AuthenticationMethod>();
        foreach (var method in _authenticationMethods.Where(x => x.SupportsMfa && x.Enabled)) {
            switch (method.Type) {
                case AuthenticationMethodType.TrustedDevice when await _userManager.GetDevicesAsync(user, UserDeviceListFilter.TrustedNativeDevices()) is { Count: > 0 }:
                case AuthenticationMethodType.AuthenticatorApp when !string.IsNullOrWhiteSpace(await _userManager.GetAuthenticatorKeyAsync(user)):
                case AuthenticationMethodType.PhoneNumber when !string.IsNullOrWhiteSpace(await _userManager.GetPhoneNumberAsync(user)) && await _userManager.IsPhoneNumberConfirmedAsync(user):
                    methods.Add(method);
                    break;
                default:
                    continue;
            }
        }
        return methods.ToArray();
    }
}
