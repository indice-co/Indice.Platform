using System.Security.Claims;
using Indice.Features.Workers.Server;
using Indice.Features.Workers.Server.Services;
using Indice.Hosting.Data;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
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
        services.AddMemoryCache();
        services.AddAuthorization();
        services.AddOptions<AuthorizationOptions>().Configure<IOptions<WorkerEndpointsOptions>>((authorization, endpointOptions) => {
            var requiredScope = endpointOptions.Value.RequiredScope;
            var producerScope = endpointOptions.Value.ProducerScope;
            var consumerScope = endpointOptions.Value.ConsumerScope;
            authorization.AddPolicy(WorkerPolicies.BeWorkersReader, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => HasRequiredScope(context.User, requiredScope) && context.User.CanReadWorkers()));
            authorization.AddPolicy(WorkerPolicies.BeWorkersAdmin, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => HasRequiredScope(context.User, requiredScope) && context.User.CanManageWorkers()));
            authorization.AddPolicy(WorkerPolicies.BeWorkersProducer, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    (HasRequiredScope(context.User, requiredScope) && context.User.CanManageWorkers()) ||
                    (HasSubScope(context.User, producerScope) && (IsMachineClient(context.User) || context.User.CanManageWorkers()))));
            authorization.AddPolicy(WorkerPolicies.BeWorkersConsumer, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => HasSubScope(context.User, consumerScope) && IsMachineClient(context.User)));
        });
        return services;
    }

    private static bool HasRequiredScope(ClaimsPrincipal user, string? requiredScope) =>
        string.IsNullOrEmpty(requiredScope) || user.HasScope(requiredScope);

    // Unlike the required scope, a sub-scope that is not configured grants nothing.
    private static bool HasSubScope(ClaimsPrincipal user, string? subScope) =>
        !string.IsNullOrEmpty(subScope) && user.HasScope(subScope);

    // A client credentials token carries no subject. The inbound claim mapping may have renamed 'sub', so both forms are checked.
    private static bool IsMachineClient(ClaimsPrincipal user) =>
        user.FindFirst(BasicClaimTypes.Subject) is null && user.FindFirst(ClaimTypes.NameIdentifier) is null;



    /// <summary>Maps the workers dashboard minimal API under <c>WorkerDashboardOptions.PathPrefix</c> (default <c>/api/workers</c>). Requires <c>AddWorkerDashboard()</c>.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application.</param>
    public static IEndpointRouteBuilder MapWorkers(this IEndpointRouteBuilder routes) {
        routes.MapWorkerDashboardApi();
        return routes;
    }
}
