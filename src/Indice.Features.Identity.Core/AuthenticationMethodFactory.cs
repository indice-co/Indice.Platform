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
    public AuthenticationMethod[] GetAll() {
        return _configurations
            .Select(CreateMethod)
            .Where(m => m != null)
            .Cast<AuthenticationMethod>()
            .OrderByDescending(x => x.SecurityLevel)
            .ToArray();
    }

    /// <inheritdoc />
    public AuthenticationMethod? GetByCode(string code) {
        return GetAll().FirstOrDefault(m => m.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public T? Get<T>() where T : AuthenticationMethod {
        var config = _configurations.FirstOrDefault(c => c.MethodType == typeof(T));
        return config != null ? CreateMethod(config) as T : null;
    }

    /// <summary>Creates an authentication method instance from configuration.</summary>
    private AuthenticationMethod? CreateMethod(AuthenticationMethodConfiguration config) {
        var method = (AuthenticationMethod?)Activator.CreateInstance(config.MethodType, _messageDescriber);
        //if (method is not null) {
        //    method.SupportsMfa = config.SupportsMfa;
        //    method.Enabled = config.Enabled;
        //}
        return method;
    }
}