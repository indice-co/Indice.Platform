using Indice.Features.ActivityLogs.Abstractions;
using Indice.Features.ActivityLogs.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.ActivityLogs.Enrichers;

internal class ApplicationInfoEnricher : IActivityLogEntryEnricher
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;


    /// <summary>Creates a new instance of <see cref="ApplicationInfoEnricher"/> class.</summary>
    /// <param name="environment">Provides access to the hosting environment details.</param>
    /// <param name="configuration">Provides access to application configuration.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ApplicationInfoEnricher(IWebHostEnvironment environment, IConfiguration configuration) {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }


    /// <inheritdoc />
    public int Order => 3;
    /// <inheritdoc />
    public ActivityLogEnricherRunType RunType => ActivityLogEnricherRunType.Synchronous;

    /// <inheritdoc />
    public async ValueTask EnrichAsync(ActivityLogEntry logEntry) {
        logEntry.ApplicationName = _environment.ApplicationName;
    }
}
