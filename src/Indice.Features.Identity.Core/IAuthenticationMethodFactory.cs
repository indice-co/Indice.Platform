using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Core;

/// <summary>
/// Factory for creating localized authentication method instances.
/// </summary>
public interface IAuthenticationMethodFactory
{
    /// <summary>Returns all configured authentication methods paired with their configuration.</summary>
    /// <returns>Array of localized authentication methods.</returns>
    AuthenticationMethodEntry[] GetAll();

    /// <summary>Returns the entry for a specific method code.</summary>
    /// <param name="code">The authentication method code (e.g., "Sms", "Email").</param>
    /// <returns>Localized authentication method or null if not found.</returns>
    AuthenticationMethodEntry? GetByCode(string code);

    /// <summary>Returns the entry for a specific method type.</summary>
    /// <typeparam name="T">The authentication method type.</typeparam>
    /// <returns>Localized authentication method or null if not registered.</returns>
    AuthenticationMethodEntry? Get<T>() where T : AuthenticationMethod;
}
