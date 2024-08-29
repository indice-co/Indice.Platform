using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Hubs;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace Indice.Features.Identity.Core;

/// <summary>Abstracts interaction with system's various authentication methods.</summary>
public interface IAuthenticationMethodProvider
{
    /// <summary>Determines if SignalR service is configured. Use <strong>AddSignalR</strong> in your Program.cs.</summary>
    public bool SignalREnabled => HubContext is not null;
    /// <summary>SignalR hub context.</summary>
    IHubContext<MultiFactorAuthenticationHub>? HubContext { get; }
    /// <summary>Gets a list of all available authentication methods supported by the identity system.</summary>
    Task<IEnumerable<AuthenticationMethod>> GetAllMethodsAsync();
    /// <summary>Get the authentication method that must be applied to the user.</summary>
    /// <param name="user">The user instance.</param>
    /// <param name="tryDowngradeAuthenticationMethod"></param>
    Task<AuthenticationMethod?> GetRequiredAuthenticationMethod(User user, bool? tryDowngradeAuthenticationMethod = false);
}
