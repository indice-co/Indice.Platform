using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a user's two-factor authentication preference is changed.</summary>
/// <remarks>Creates a new instance of <see cref="TwoFactorPreferenceChangedEvent"/>.</remarks>
/// <param name="user">The user context for whom the two-factor preference changed.</param>
/// <param name="authenticationMethodCode">The two-factor authentication method code.</param>
public class TwoFactorPreferenceChangedEvent(UserEventContext user, string authenticationMethodCode) : IPlatformEvent
{
    /// <summary>The two-factor authentication method code.</summary>
    public string AuthenticationMethodCode { get; } = authenticationMethodCode;
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;
}
