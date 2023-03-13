using IdentityServer4.Events;
using Indice.Features.Identity.Core.Logging.Models;

namespace Indice.Features.Identity.Core.Logging;

/// <summary>A factory class in order to create <see cref="SignInLogEntry"/> instances.</summary>
internal class SignInLogEntryAdapterFactory
{
    /// <summary>Creates an <see cref="SignInLogEntry"/> instance given an <see cref="Event"/> instance.</summary>
    /// <param name="event">Models base class for events raised from IdentityServer.</param>
    public static SignInLogEntry Create(Event @event) {
        if (@event is null) {
            return default;
        }
        return @event switch {
            TokenIssuedSuccessEvent => SignInLogEntryFactory.CreateFromTokenIssuedSuccessEvent((TokenIssuedSuccessEvent)@event),
            TokenIssuedFailureEvent => SignInLogEntryFactory.CreateFromTokenIssuedFailureEvent((TokenIssuedFailureEvent)@event),
            UserLoginSuccessEvent => SignInLogEntryFactory.CreateFromUserLoginSuccessEvent((UserLoginSuccessEvent)@event),
            UserLoginFailureEvent => SignInLogEntryFactory.CreateFromUserLoginFailureEvent((UserLoginFailureEvent)@event),
            _ => null
        };
    }
}
