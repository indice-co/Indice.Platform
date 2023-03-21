using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs;

internal class SignInLogManager
{
    public SignInLogManager(
        IEnumerable<ISignInLogEntryEnricher> enrichers,
        IEnumerable<ISignInLogEntryFilter> filters,
        ISignInLogStore store
    ) {
        Store = store ?? throw new ArgumentNullException(nameof(store));
        foreach (var enricher in enrichers.OrderBy(x => x.Priority)) {
            Enrichers.Add(enricher);
        }
        foreach (var filter in filters) {
            Filters.Add(filter);
        }
    }

    public IList<ISignInLogEntryEnricher> Enrichers { get; } = new List<ISignInLogEntryEnricher>();
    public IList<ISignInLogEntryFilter> Filters { get; } = new List<ISignInLogEntryFilter>();
    public ISignInLogStore Store { get; }

    public async Task CreateAsync(SignInLogEntry logEntry) {
        var discard = await MustDiscard(logEntry);
        if (discard) {
            return;
        }
        foreach (var enricher in Enrichers) {
            await enricher.Enrich(logEntry);
        }
        await Store.CreateAsync(logEntry);
    }

    private async Task<bool> MustDiscard(SignInLogEntry logEntry) {
        var discard = false;
        foreach (var filter in Filters) {
            discard = await filter.Discard(logEntry);
            // If one of the filters dictates that we must discard the log entry then do not proceed with other filters.
            if (discard) {
                break;
            }
        }
        return discard;
    }
}
