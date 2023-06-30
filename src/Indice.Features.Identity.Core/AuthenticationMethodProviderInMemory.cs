using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Hubs;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core;

/// <summary>Default implementation of <see cref="IAuthenticationMethodProvider"/> where authentication methods are statically configured.</summary>
public class AuthenticationMethodProviderInMemory : IAuthenticationMethodProvider
{
    private readonly IEnumerable<AuthenticationMethod> _authenticationMethods;
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
        _authenticationMethods = authenticationMethods ?? throw new ArgumentNullException(nameof(authenticationMethods));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        HubContext = multiFactorAuthenticationHubs?.FirstOrDefault();
    }

    /// <inheritdoc />
    public IHubContext<MultiFactorAuthenticationHub> HubContext { get; }

    /// <inheritdoc />
    public Task<IEnumerable<AuthenticationMethod>> GetAllMethodsAsync() => Task.FromResult(_authenticationMethods);

    /// <inheritdoc />
    /// <remarks>For now the supported authentication methods are <see cref="SmsAuthenticationMethod"/> and <see cref="BiometricsAuthenticationMethod"/>.</remarks>
    public async Task<AuthenticationMethod> GetRequiredAuthenticationMethod(User user, bool? tryDowngradeAuthenticationMethod = false) {
        if (_authenticationMethods?.Count() == 0) {
            throw new InvalidOperationException("No authentication methods have been configured.");
        }
        if (_authenticationMethods?.Count() == 1) {
            return _authenticationMethods.Single();
        }
        var selectedAuthenticationMethod = _authenticationMethods.FirstOrDefault(x => x.Type == AuthenticationMethodType.PhoneNumber);
        var allowMfaChannelDowngrade = _configuration.GetIdentityOption<bool?>($"{nameof(IdentityOptions.SignIn)}:Mfa", "AllowDowngradeAuthenticationMethod") ?? false;
        if ((tryDowngradeAuthenticationMethod ??= false) && allowMfaChannelDowngrade) {
            return selectedAuthenticationMethod;
        }
        var trustedDevices = await _userManager.GetDevicesAsync(user, UserDeviceListFilter.TrustedNativeDevices());
        if (trustedDevices.Count > 0) {
            selectedAuthenticationMethod = _authenticationMethods.FirstOrDefault(x => x.Type == AuthenticationMethodType.Biometrics);
            return selectedAuthenticationMethod;
        }
        var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        var phoneNumberConfirmed = !string.IsNullOrWhiteSpace(phoneNumber) && await _userManager.IsPhoneNumberConfirmedAsync(user);
        if (phoneNumberConfirmed) {
            return selectedAuthenticationMethod;
        }
        return null;
    }
}
