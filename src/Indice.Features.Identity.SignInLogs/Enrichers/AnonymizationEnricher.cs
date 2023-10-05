using System.Net;
using Indice.Features.Identity.SignInLogs.Abstractions;
using Indice.Features.Identity.SignInLogs.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.SignInLogs.Enrichers;

/// <summary>An enricher that anonymizes sensitive user data (i.e. IP address).</summary>
public sealed class AnonymizationEnricher : ISignInLogEntryEnricher
{
    private readonly SignInLogOptions _signInLogOptions;

    /// <summary>Creates a new instance of <see cref="AnonymizationEnricher"/> class.</summary>
    /// <param name="signInLogOptions">Options for configuring the IdentityServer sign in logs mechanism.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AnonymizationEnricher(IOptions<SignInLogOptions> signInLogOptions) {
        _signInLogOptions = signInLogOptions?.Value ?? throw new ArgumentNullException(nameof(signInLogOptions));
    }

    /// <inheritdoc />
    public int Order => int.MaxValue;
    /// <inheritdoc />
    public SignInLogEnricherRunType RunType => SignInLogEnricherRunType.Default;

    /// <inheritdoc />
    public ValueTask EnrichAsync(SignInLogEntry logEntry) {
        if (!_signInLogOptions.AnonymizePersonalData) {
            return ValueTask.CompletedTask;
        }
        logEntry.IpAddress = IPAddress.Any.ToString();
        return ValueTask.CompletedTask;
    }
}