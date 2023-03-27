using Indice.Features.Identity.Core.Models;

namespace Indice.Features.Identity.Core;

/// <summary>Abstracts interaction with system's various authentication methods.</summary>
public interface IAuthenticationMethodProvider
{
    /// <summary>Gets a list of all available authentication methods supported by the identity system.</summary>
    Task<IEnumerable<AuthenticationMethod>> GetAllMethodsAsync();
}
