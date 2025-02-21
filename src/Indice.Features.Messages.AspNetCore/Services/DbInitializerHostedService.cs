using Indice.Features.Media.Data;
using Indice.Features.Messages.Core.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Media.AspNetCore.Services.Hosting;

/// <summary>
/// This service will be registered only if hosting environment is set at <strong>Developement</strong> in order to ensure the database is created.
/// </summary>
internal class DbInitializerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DbInitializerHostedService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="DbInitializerHostedService"/>
    /// </summary>
    /// <param name="serviceScopeFactory">The service provider factory. Used to create scopes</param>
    /// <param name="environment">The service environment</param>
    /// <param name="logger">a logger</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbInitializerHostedService(IServiceScopeFactory serviceScopeFactory, IHostEnvironment environment, ILogger<DbInitializerHostedService> logger) {
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

        try {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
            await context.Database.EnsureCreatedAsync();
        } catch (DbUpdateException ex) {
            _logger.LogError(ex, "DbInitializerHostedService Database update failed for CampaignsDbContext");
        } catch (InvalidOperationException ex) {
            _logger.LogError(ex, "DbInitializerHostedService Invalid operation in CampaignsDbContext");
        }

        try {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            await context.Database.MigrateAsync();
            RelationalDatabaseCreator databaseCreator = (RelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();
            await databaseCreator.CreateTablesAsync();
        } catch (SqlException ex) {
            _logger.LogError(ex, "DbInitializerHostedService. Database update failed for MediaDbContext");
        } catch (DbUpdateException ex) {
            _logger.LogError(ex, "DbInitializerHostedService. Database update failed for MediaDbContext");
        } catch (InvalidOperationException ex) {
            _logger.LogError(ex, "DbInitializerHostedService. Invalid operation in MediaDbContext");
        } 
    }
}