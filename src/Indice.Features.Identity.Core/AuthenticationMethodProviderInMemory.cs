using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Hubs;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;

namespace Indice.Features.Identity.Core;

/// <summary>Default implementation of <see cref="IAuthenticationMethodProvider"/> where authentication methods are created via factory.</summary>
public class AuthenticationMethodProviderInMemory : IAuthenticationMethodProvider
{
    private readonly IConfiguration _configuration;
    private readonly ExtendedUserManager<User> _userManager;
    private readonly IReadOnlyCollection<AuthenticationMethodEntry> _authenticationMethods;

    /// <summary>Creates a new instance of <see cref="AuthenticationMethodProviderInMemory"/>.</summary>
    /// <param name="multiFactorAuthenticationHubs">SignalR hub contexts for MFA.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <param name="authenticationMethods">A collection of authentication methods.</param>
    /// <param name="configurations">A collection of authentication method configurations.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AuthenticationMethodProviderInMemory(
        IEnumerable<IHubContext<MultiFactorAuthenticationHub>> multiFactorAuthenticationHubs,
        IConfiguration configuration,
        ExtendedUserManager<User> userManager,
        IEnumerable<AuthenticationMethod> authenticationMethods,
        IEnumerable<AuthenticationMethodConfiguration> configurations
    ) {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));

        // Lazy initialization - methods are created only when first accessed
        _authenticationMethods = configurations.Join(authenticationMethods, 
                                                     c => c.MethodType, 
                                                     m => m.GetType(), 
                                                     (c, m) => new AuthenticationMethodEntry(m, c))
                                                .OrderByDescending(e => e.Method.SecurityLevel)
                                                .ToArray();
        HubContext = multiFactorAuthenticationHubs?.FirstOrDefault();
        AllowMfaChannelDowngrade = _configuration.GetIdentityOption<bool>($"{nameof(IdentityOptions.SignIn)}:Mfa", "AllowDowngradeAuthenticationMethod");
    }

    /// <inheritdoc />
    public IHubContext<MultiFactorAuthenticationHub>? HubContext { get; }

    /// <inheritdoc />
    public bool AllowMfaChannelDowngrade { get; }

    /// <inheritdoc />
    public Task<AuthenticationMethod[]> GetAllMethodsAsync() => Task.FromResult(_authenticationMethods.Select(e => e.Method).ToArray());

    /// <inheritdoc />
    /// <remarks>For now the supported authentication methods are <see cref="SmsAuthenticationMethod"/>, <see cref="TrustedDeviceAuthenticationMethod"/> and <see cref="AuthenticatorAppAuthenticationMethod"/>.</remarks>
    public async Task<AuthenticationMethod?> FindMethodForUserOrDefaultAsync(User user, string? code = null) {
        var userMethods = await GetAllMethodsForUserAsync(user);
        if (!string.IsNullOrEmpty(code) && AllowMfaChannelDowngrade) {
            var byCode = userMethods.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
            if (byCode is not null) {
                return byCode;
            }
        }
        return userMethods.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<AuthenticationMethod[]> GetAllMethodsForUserAsync(User user) {
        var methods = new List<AuthenticationMethod>();
        foreach (var entry in _authenticationMethods.Where(x => x.SupportsMfa && x.Enabled)) {
            switch (entry.Method.Type) {
                case AuthenticationMethodType.TrustedDevice when await _userManager.GetDevicesAsync(user, UserDeviceListFilter.TrustedNativeDevices()) is { Count: > 0 }:
                case AuthenticationMethodType.AuthenticatorApp when !string.IsNullOrWhiteSpace(await _userManager.GetAuthenticatorKeyAsync(user)):
                case AuthenticationMethodType.RecoveryCode when !string.IsNullOrWhiteSpace(await _userManager.GetAuthenticatorKeyAsync(user)):
                case AuthenticationMethodType.PhoneNumber when !string.IsNullOrWhiteSpace(await _userManager.GetPhoneNumberAsync(user)) && await _userManager.IsPhoneNumberConfirmedAsync(user):
                    methods.Add(entry.Method);
                    break;
                case AuthenticationMethodType.Email when !string.IsNullOrWhiteSpace(await _userManager.GetEmailAsync(user)) && await _userManager.IsEmailConfirmedAsync(user):
                    methods.Add(entry.Method);
                    break;
                default:
                    continue;
            }
        }
        return methods.ToArray();
    }
}