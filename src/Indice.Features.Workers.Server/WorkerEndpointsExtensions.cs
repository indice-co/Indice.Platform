using Indice.Features.Workers.Server;
using Indice.Features.Workers.Server.Services;
using Indice.Hosting.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the workers dashboard API.</summary>
public static class WorkerEndpointsExtensions
{
    /// <summary>Adds the services of the workers dashboard API, reading the worker tables through the built-in <see cref="TaskDbContext"/>.</summary>
    /// <remarks>
    /// The dashboard is meant to run in a host that does <b>not</b> call <c>AddWorkerHost()</c>, so it registers its own <see cref="ITaskDbContext"/> and <see cref="ILockDbContext"/>.
    /// The database is configured through <see cref="WorkerEndpointsOptions.ConfigureDbContext"/>, or the <c>WorkerDb</c> connection string by default.
    /// </remarks>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureOptions">Configures the <see cref="WorkerEndpointsOptions"/>.</param>
    public static IServiceCollection AddWorkersEndpoints(this IServiceCollection services, Action<WorkerEndpointsOptions>? configureOptions = null) =>
        services.AddWorkerEndpoints<TaskDbContext>(configureOptions);

    /// <summary>Adds the services of the workers dashboard API, reading the worker tables through <typeparamref name="TContext"/>.</summary>
    /// <remarks>Use this overload when the worker tables live in your own <see cref="DbContext"/>, the one passed to <c>UseStoreRelational&lt;TContext&gt;()</c> by the workers.</remarks>
    /// <typeparam name="TContext">The <see cref="DbContext"/> that maps the worker tables (see <c>ApplyWorkerConfiguration</c>).</typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureOptions">Configures the <see cref="WorkerEndpointsOptions"/>.</param>
    public static IServiceCollection AddWorkerEndpoints<TContext>(this IServiceCollection services, Action<WorkerEndpointsOptions>? configureOptions = null) where TContext : DbContext, ITaskDbContext, ILockDbContext {
        var builder = services.AddOptions<WorkerEndpointsOptions>();
        if (configureOptions is not null) {
            builder.Configure(configureOptions);
        }
        // Same registration shape as UseStoreRelational<TContext>(), so a host that also publishes work keeps a single context per scope.
        if (!services.Any(sd => sd.ServiceType == typeof(TContext))) {
            services.AddDbContext<TContext>((serviceProvider, dbBuilder) => {
                var configureDbContext = serviceProvider.GetRequiredService<IOptions<WorkerEndpointsOptions>>().Value.ConfigureDbContext;
                if (configureDbContext is not null) {
                    configureDbContext(dbBuilder);
                } else {
                    dbBuilder.UseSqlServer(serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("WorkerDb"));
                }
            });
        }
        services.TryAddScoped<ITaskDbContext>(serviceProvider => serviceProvider.GetRequiredService<TContext>());
        services.TryAddScoped<ILockDbContext>(serviceProvider => serviceProvider.GetRequiredService<TContext>());
        services.AddEndpointParameterFluentValidation(typeof(WorkerEndpointsExtensions).Assembly);
        services.TryAddScoped<IWorkerDashboardService, WorkerDashboardService>();
        return services;
    }



    /// <summary>Maps the workers dashboard minimal API under <c>WorkerDashboardOptions.PathPrefix</c> (default <c>/api/workers</c>). Requires <c>AddWorkerDashboard()</c>.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application.</param>
    public static IEndpointRouteBuilder MapWorkers(this IEndpointRouteBuilder routes) {
        routes.MapWorkerDashboardApi();
        return routes;
    }
}
