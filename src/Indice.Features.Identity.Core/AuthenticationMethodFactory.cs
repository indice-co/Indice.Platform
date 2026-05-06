using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Core;

/// <summary>
/// Default implementation of <see cref="IAuthenticationMethodFactory"/> that creates localized authentication methods.
/// </summary>
public class AuthenticationMethodFactory : IAuthenticationMethodFactory
{
    private readonly IdentityMessageDescriber _messageDescriber;
    private readonly IReadOnlyList<AuthenticationMethodConfiguration> _configurations;

    /// <summary>Creates a new instance of <see cref="AuthenticationMethodFactory"/>.</summary>
    /// <param name="configurations">The authentication method configurations.</param>
    /// <param name="messageDescriber">The message describer for localized strings (optional).</param>
    public AuthenticationMethodFactory(
        IEnumerable<AuthenticationMethodConfiguration> configurations,
        IdentityMessageDescriber messageDescriber) {
        _configurations = configurations?.ToList() ?? throw new ArgumentNullException(nameof(configurations));
        _messageDescriber = messageDescriber;
    }

    /// <inheritdoc />
    public AuthenticationMethodEntry[] GetAll() {
        return _configurations
            .Select(CreateEntry)
            .Where(e => e is not null)
            .Cast<AuthenticationMethodEntry>()
            .OrderByDescending(e => e.Method.SecurityLevel)
            .ToArray();
    }

    /// <inheritdoc />
    public AuthenticationMethodEntry? GetByCode(string code) =>
        GetAll().FirstOrDefault(e => e.Method.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public AuthenticationMethodEntry? Get<T>() where T : AuthenticationMethod {
        var config = _configurations.FirstOrDefault(c => c.MethodType == typeof(T));
        return config is not null ? CreateEntry(config) : null;
    }

    /// <summary>Creates an authentication method instance from configuration.</summary>
    private AuthenticationMethodEntry? CreateEntry(AuthenticationMethodConfiguration config) {
        var method = (AuthenticationMethod?)Activator.CreateInstance(config.MethodType, _messageDescriber);
        return method is not null ? new AuthenticationMethodEntry(method, config) : null;
    }
}