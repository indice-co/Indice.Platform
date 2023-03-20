using IdentityServer4.Events;
using IdentityServer4.Services;

namespace Indice.Features.Identity.SignInLogs;

/// <summary>IdentityServer event sink in order to persist data for a sign in event.</summary>
internal class SignInLogEventSink : IEventSink
{
    private readonly SignInLogManager _signInLogManager;

    /// <summary>Creates a new instance of <see cref="SignInLogEventSink"/> class.</summary>
    /// <param name="signInLogManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SignInLogEventSink(SignInLogManager signInLogManager) {
        _signInLogManager = signInLogManager ?? throw new ArgumentNullException(nameof(signInLogManager));
    }

    /// <inheritdoc />
    public Task PersistAsync(Event @event) {
        var logEntry = SignInLogEntryAdapterFactory.Create(@event);
        if (logEntry is null) {
            return Task.CompletedTask;
        }
        return _signInLogManager.CreateAsync(logEntry);
    }
}
