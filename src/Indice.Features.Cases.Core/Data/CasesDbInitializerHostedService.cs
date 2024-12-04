using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Data;

/// <summary>
/// This service will be registered only if hosting environment is set at <strong>Developement</strong> in order to ensure the database is created.
/// </summary>
internal class CasesDbInitializerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CasesDbInitializerHostedService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="CasesDbInitializerHostedService"/>
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public CasesDbInitializerHostedService(IServiceProvider provider, ILogger<CasesDbInitializerHostedService> logger) {
        _serviceProvider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the background service's logic.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var hostingEnvironment = _serviceProvider.GetRequiredService<IHostEnvironment>();
        if (!hostingEnvironment.IsDevelopment()) {
            return;
        }
        await Task.Delay(TimeSpan.FromSeconds(2));
        using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CasesDbContext>();
        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<CasesDbIntialDataOptions>>();
        await dbContext.InitializeAsync(seedOptions);
    }
}