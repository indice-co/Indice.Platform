using Indice.Hosting.Data;
using Indice.Hosting.UI;
using Indice.Hosting.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the workers dashboard API.</summary>
public static class WorkerDashboardFeatureExtensions
{
    /// <summary>Adds the services of the workers dashboard API, reading the worker tables through the built-in <see cref="TaskDbContext"/>.</summary>
    /// <remarks>
    /// The dashboard is meant to run in a host that does <b>not</b> call <c>AddWorkerHost()</c>, so it registers its own <see cref="ITaskDbContext"/> and <see cref="ILockDbContext"/>.
    /// The database is configured through <see cref="WorkerDashboardOptions.ConfigureDbContext"/>, or the <c>WorkerDb</c> connection string by default.
    /// </remarks>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureOptions">Configures the <see cref="WorkerDashboardOptions"/>.</param>
    public static IServiceCollection AddWorkerDashboard(this IServiceCollection services, Action<WorkerDashboardOptions>? configureOptions = null) =>
        services.AddWorkerDashboard<TaskDbContext>(configureOptions);

    /// <summary>Adds the services of the workers dashboard API, reading the worker tables through <typeparamref name="TContext"/>.</summary>
    /// <remarks>Use this overload when the worker tables live in your own <see cref="DbContext"/>, the one passed to <c>UseStoreRelational&lt;TContext&gt;()</c> by the workers.</remarks>
    /// <typeparam name="TContext">The <see cref="DbContext"/> that maps the worker tables (see <c>ApplyWorkerConfiguration</c>).</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureOptions">Configures the <see cref="WorkerDashboardOptions"/>.</param>
    public static IServiceCollection AddWorkerDashboard<TContext>(this IServiceCollection services, Action<WorkerDashboardOptions>? configureOptions = null) where TContext : DbContext, ITaskDbContext, ILockDbContext {
        var builder = services.AddOptions<WorkerDashboardOptions>();
        if (configureOptions is not null) {
            builder.Configure(configureOptions);
        }
        // Same registration shape as UseStoreRelational<TContext>(), so a host that also publishes work keeps a single context per scope.
        if (!services.Any(sd => sd.ServiceType == typeof(TContext))) {
            services.AddDbContext<TContext>((serviceProvider, dbBuilder) => {
                var configureDbContext = serviceProvider.GetRequiredService<IOptions<WorkerDashboardOptions>>().Value.ConfigureDbContext;
                if (configureDbContext is not null) {
                    configureDbContext(dbBuilder);
                } else {
                    dbBuilder.UseSqlServer(serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("WorkerDb"));
                }
            });
        }
        services.TryAddScoped<ITaskDbContext>(serviceProvider => serviceProvider.GetRequiredService<TContext>());
        services.TryAddScoped<ILockDbContext>(serviceProvider => serviceProvider.GetRequiredService<TContext>());
        services.AddEndpointParameterFluentValidation(typeof(WorkerDashboardFeatureExtensions).Assembly);
        services.TryAddScoped<IWorkerDashboardService, WorkerDashboardService>();
        return services;
    }
}
