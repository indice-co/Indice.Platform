using IdentityServer4.Events;
using IdentityServer4.Services;
using Indice.Features.Identity.SignInLogs.Abstractions;

namespace Indice.Features.Identity.SignInLogs;

/// <summary>IdentityServer event sink in order to persist data for a sign in event.</summary>
public class SignInLogEventSink : IEventSink
{
    private readonly ISignInLogService _logService;
    private readonly IEnumerable<ISignInLogEntryEnricher> _enrichers;

    /// <summary>Creates a new instance of <see cref="SignInLogEventSink"/> class.</summary>
    /// <param name="logService">A service that contains operations used to persist the audit data of an event.</param>
    /// <param name="enrichers"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SignInLogEventSink(
        ISignInLogService logService,
        IEnumerable<ISignInLogEntryEnricher> enrichers
    ) {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _enrichers = enrichers?.ToArray() ?? throw new ArgumentNullException(nameof(enrichers));
    }

    /// <inheritdoc />
    public async Task PersistAsync(Event @event) {
        var signInEvent = SignInLogEntryAdapterFactory.Create(@event);
        if (signInEvent is null) {
            return;
        }
        foreach (var enricher in _enrichers) {
            await enricher.Enrich(signInEvent);
        }
        if (signInEvent is not null) {
            await _logService.CreateAsync(signInEvent);
        }
    }
}
