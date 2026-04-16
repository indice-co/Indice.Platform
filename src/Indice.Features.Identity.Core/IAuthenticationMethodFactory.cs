using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Core;

/// <summary>
/// Factory for creating localized authentication method instances.
/// </summary>
public interface IAuthenticationMethodFactory
{
    /// <summary>Gets all configured authentication methods with localized display names and descriptions.</summary>
    /// <returns>Array of localized authentication methods.</returns>
    AuthenticationMethod[] GetAll();

    /// <summary>Creates a localized authentication method by its code.</summary>
    /// <param name="code">The authentication method code (e.g., "Sms", "Email").</param>
    /// <returns>Localized authentication method or null if not found.</returns>
    AuthenticationMethod? GetByCode(string code);

    /// <summary>Creates a localized authentication method by type.</summary>
    /// <typeparam name="T">The authentication method type.</typeparam>
    /// <returns>Localized authentication method or null if not registered.</returns>
    T? Get<T>() where T : AuthenticationMethod;
}
