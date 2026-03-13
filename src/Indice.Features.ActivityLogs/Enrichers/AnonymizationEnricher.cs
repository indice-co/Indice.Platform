using System.Net;
using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.ActivityLogs.Enrichers;

/// <summary>An enricher that anonymizes sensitive user data (i.e. IP address).</summary>
public sealed class AnonymizationEnricher : IActivityLogEntryEnricher
{
    private readonly ActivityLogOptions _ActivityLogOptions;

    /// <summary>Creates a new instance of <see cref="AnonymizationEnricher"/> class.</summary>
    /// <param name="ActivityLogOptions">Options for configuring the IdentityServer activity logs mechanism.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AnonymizationEnricher(IOptions<ActivityLogOptions> ActivityLogOptions) {
        _ActivityLogOptions = ActivityLogOptions?.Value ?? throw new ArgumentNullException(nameof(ActivityLogOptions));
    }

    /// <inheritdoc />
    public int Order => int.MaxValue;
    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Default;

    /// <inheritdoc />
    public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        if (!_ActivityLogOptions.AnonymizePersonalData) {
            return ValueTask.CompletedTask;
        }
        logEntry.IpAddress = IPAddress.Any.ToString();
        return ValueTask.CompletedTask;
    }
}