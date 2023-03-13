using IdentityServer4.Events;
using IdentityServer4.Services;
using Indice.Features.Identity.Core.Logging.Abstractions;

namespace Indice.Features.Identity.Core.Logging;

/// <summary>IdentityServer event sink in order to persist data for a sign in event.</summary>
public class SignInLogEventSink : IEventSink
{
    private readonly ISignInLogService _logService;

    /// <summary>Creates a new instance of <see cref="SignInLogEventSink"/> class.</summary>
    /// <param name="logService">A service that contains operations used to persist the audit data of an event.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SignInLogEventSink(ISignInLogService logService) {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
    }

    /// <inheritdoc />
    public async Task PersistAsync(Event @event) {
        var concreteEvent = SignInLogEntryAdapterFactory.Create(@event);
        if (concreteEvent is not null) {
            await _logService.CreateAsync(concreteEvent);
        }
    }
}
