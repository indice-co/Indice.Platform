using Indice.Features.Workers.Server.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Health checks over the worker tables. They need an <c>ITaskDbContext</c>, registered by <c>AddWorkerEndpoints()</c> or by <c>UseStoreRelational()</c> of the workers.</summary>
public static class WorkerHealthCheckExtensions
{
    /// <summary>Adds a health check that reports the poison messages when they are more than a threshold.</summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name. Defaults to <c>worker-poison-messages</c>.</param>
    /// <param name="configure">Configures the <see cref="WorkerPoisonMessagesHealthCheckOptions"/>.</param>
    /// <param name="failureStatus">The status to report above the threshold. Defaults to <see cref="HealthStatus.Degraded"/>.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <param name="timeout">An optional timeout of the check.</param>
    public static IHealthChecksBuilder AddWorkerPoisonMessages(
        this IHealthChecksBuilder builder,
        string name = "worker-poison-messages",
        Action<WorkerPoisonMessagesHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = HealthStatus.Degraded,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        builder.Services.AddOptions<WorkerPoisonMessagesHealthCheckOptions>(name);
        if (configure is not null) {
            builder.Services.Configure(name, configure);
        }
        return builder.AddCheck<WorkerPoisonMessagesHealthCheck>(name, failureStatus, tags ?? [], timeout);
    }

    /// <summary>Adds a health check that reports scheduled tasks that have been running for too long, and optionally the ones that missed their schedule.</summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name. Defaults to <c>worker-stuck-tasks</c>.</param>
    /// <param name="configure">Configures the <see cref="WorkerStuckTasksHealthCheckOptions"/>.</param>
    /// <param name="failureStatus">The status to report when a task is stuck. Defaults to <see cref="HealthStatus.Degraded"/>.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <param name="timeout">An optional timeout of the check.</param>
    public static IHealthChecksBuilder AddWorkerStuckTasks(
        this IHealthChecksBuilder builder,
        string name = "worker-stuck-tasks",
        Action<WorkerStuckTasksHealthCheckOptions>? configure = null,
        HealthStatus? failureStatus = HealthStatus.Degraded,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        builder.Services.AddOptions<WorkerStuckTasksHealthCheckOptions>(name);
        if (configure is not null) {
            builder.Services.Configure(name, configure);
        }
        return builder.AddCheck<WorkerStuckTasksHealthCheck>(name, failureStatus, tags ?? [], timeout);
    }
}
