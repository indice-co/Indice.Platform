using Indice.Features.Identity.Core.Hubs;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace Indice.Features.Identity.Core;

/// <summary>Default implementation of <see cref="IAuthenticationMethodProvider"/> where authentication methods are statically configured.</summary>
public class AuthenticationMethodProviderInMemory : IAuthenticationMethodProvider
{
    private readonly IEnumerable<AuthenticationMethod> _authenticationMethods;

    /// <summary>Creates a new instance of <see cref="AuthenticationMethodProviderInMemory"/>.</summary>
    /// <param name="authenticationMethods">The authentication methods to apply in the identity system.</param>
    /// <param name="multiFactorAuthenticationHub"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public AuthenticationMethodProviderInMemory(
        IEnumerable<AuthenticationMethod> authenticationMethods,
        IEnumerable<IHubContext<MultiFactorAuthenticationHub>> multiFactorAuthenticationHub
    ) {
        _authenticationMethods = authenticationMethods ?? throw new ArgumentNullException(nameof(authenticationMethods));
        HubContext = multiFactorAuthenticationHub?.FirstOrDefault() ?? throw new ArgumentNullException(nameof(multiFactorAuthenticationHub));
    }

    /// <inheritdoc />
    public IHubContext<MultiFactorAuthenticationHub> HubContext { get; }

    /// <inheritdoc />
    public Task<IEnumerable<AuthenticationMethod>> GetAllMethodsAsync() => Task.FromResult(_authenticationMethods);
}
