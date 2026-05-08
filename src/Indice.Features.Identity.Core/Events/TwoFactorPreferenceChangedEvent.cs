using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a device is updated through <see cref="ExtendedUserManager{User}"/>.</summary>
/// <remarks>Creates a new instance of <see cref="TwoFactorPreferenceChangedEvent"/>.</remarks>
/// <param name="user">The user context.</param>
/// <param name="authenticationMethodCode">Two factor method</param>
public class TwoFactorPreferenceChangedEvent(UserEventContext user, string authenticationMethodCode) : IPlatformEvent
{
    /// <summary>The device context.</summary>
    public string AuthenticationMethodCode { get; } = authenticationMethodCode;
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;
}
