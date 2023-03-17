using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

internal class AnonymizationEnricher : ISignInLogEntryEnricher
{
    private readonly SignInLogOptions _options;

    public AnonymizationEnricher(IOptions<SignInLogOptions> options) {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public int Order => 6;

    public Task Enrich(SignInLogEntry logEntry) {
        if (!_options.AnonymizePersonalData) {
            return Task.CompletedTask;
        }
        logEntry.IpAddress = "0.0.0.0";
        return Task.CompletedTask;
    }
}
