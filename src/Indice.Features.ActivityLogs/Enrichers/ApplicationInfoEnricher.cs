using Indice.Features.ActivityLogs.Models;
using Microsoft.AspNetCore.Hosting;

namespace Indice.Features.ActivityLogs.Enrichers;

internal class ApplicationInfoEnricher : IActivityLogEntryEnricher
{
    private readonly IWebHostEnvironment _environment;
    /// <summary>Creates a new instance of <see cref="ApplicationInfoEnricher"/> class.</summary>
    /// <param name="environment">Provides access to the hosting environment details.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ApplicationInfoEnricher(IWebHostEnvironment environment) {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }


    /// <inheritdoc />
    public int Order => 3;
    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        logEntry.Source ??= _environment.ApplicationName;
        return ValueTask.CompletedTask;
    }
}
