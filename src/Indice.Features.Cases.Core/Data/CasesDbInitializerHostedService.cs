using System;
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
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<CasesDbInitializerHostedService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="CasesDbInitializerHostedService"/>
    /// </summary>
    /// <param name="serviceScopeFactory">The service provider factory. Used to create scopes</param>
    /// <param name="environment">The service environment</param>
    /// <param name="logger">a logger</param>
    /// <exception cref="ArgumentNullException"></exception>
    public CasesDbInitializerHostedService(IServiceScopeFactory serviceScopeFactory, IHostEnvironment environment, ILogger<CasesDbInitializerHostedService> logger) {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the background service's logic.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!_environment.IsDevelopment()) {
            return;
        }
        await Task.Delay(TimeSpan.FromSeconds(2));
        try {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CasesDbContext>();
            var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<CasesDbIntialDataOptions>>();
            await dbContext.InitializeAsync(seedOptions);
        } catch (Exception ex){
            _logger.LogError(ex, "Failed to run CasesDbInitializer");
        }
    }
}