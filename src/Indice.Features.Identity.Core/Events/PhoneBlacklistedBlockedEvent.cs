using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>
/// Raised when a user registration or update is blocked because the phone number is blacklisted.
/// </summary>
public class PhoneBlacklistedBlockedEvent(UserEventContext user) : IPlatformEvent
{
    /// <summary>The user context.</summary>
    public UserEventContext User { get; } = user;
}