#if NET9_0_OR_GREATER
using Duende.IdentityServer.Events;
#else
using IdentityServer4.Events;
#endif
using Indice.Features.Identity.Core.Events;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

/// <summary>A factory class in order to create <see cref="ActivityLogEntry"/> instances.</summary>
internal class ActivityLogEntryAdapterFactory
{
    /// <summary>Creates an <see cref="ActivityLogEntry"/> instance given an <see cref="Event"/> instance.</summary>
    /// <param name="event">Models base class for events raised from IdentityServer.</param>
    public static ActivityLogEntry? Create(Event @event) {
        if (@event is null) {
            return default;
        }
        return @event switch {
            ExtendedUserLoginSuccessEvent => ActivityLogEntryFactory.CreateFromUserLoginSuccessEvent((ExtendedUserLoginSuccessEvent)@event),
            UserPasswordLoginSuccessEvent => ActivityLogEntryFactory.CreateFromUserPasswordLoginSuccessEvent((UserPasswordLoginSuccessEvent)@event),
            _ => null
        };
    }
}
