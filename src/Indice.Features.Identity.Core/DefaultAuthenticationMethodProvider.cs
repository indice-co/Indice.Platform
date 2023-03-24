using Indice.Features.Identity.Core.Models;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core;

/// <summary>Default implementation of <see cref="IAuthenticationMethodProvider"/> where authentication methods are extracted from application settings.</summary>
public class DefaultAuthenticationMethodProvider : IAuthenticationMethodProvider
{
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="DefaultAuthenticationMethodProvider"/>.</summary>
    public DefaultAuthenticationMethodProvider(IConfiguration configuration) {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public Task<IEnumerable<AuthenticationMethod>> GetMethodsAsync() => Task.FromResult(Enumerable.Empty<AuthenticationMethod>());
}
