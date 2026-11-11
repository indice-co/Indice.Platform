using Indice.Hosting.Data;
using Indice.Hosting.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Indice.Features.Workers.Server.HealthChecks;

/// <summary>Options of the <see cref="WorkerStuckTasksHealthCheck"/>.</summary>
public class WorkerStuckTasksHealthCheckOptions
{
    /// <summary>A task that is still running this long after it started is stuck. Defaults to 1 hour.</summary>
    public TimeSpan StuckAfter { get; set; } = TimeSpan.FromHours(1);
    /// <summary>
    /// When set, an enabled task whose next execution is this much in the past is reported as overdue, which usually means that no worker hosts it anymore.
    /// Defaults to <see langword="null"/> (off), because the row of a task that was removed from the code stays in the table.
    /// </summary>
    public TimeSpan? OverdueAfter { get; set; }
    /// <summary>Ignores these task ids.</summary>
    public ICollection<string> IgnoredTaskIds { get; set; } = [];
}

/// <summary>Reports scheduled tasks that have been running for too long, and optionally the ones that missed their schedule.</summary>
/// <param name="dbContext">The worker tables.</param>
/// <param name="options">The options, per health check registration name.</param>
public class WorkerStuckTasksHealthCheck(ITaskDbContext dbContext, IOptionsMonitor<WorkerStuckTasksHealthCheckOptions> options) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        var settings = options.Get(context.Registration.Name);
        var now = DateTimeOffset.UtcNow;
        var stuckBefore = now - settings.StuckAfter;
        var ignored = settings.IgnoredTaskIds.ToList();
        try {
            var stuck = await dbContext.Tasks
                .AsNoTracking()
                .Where(x => x.Status == ScheduledTaskStatus.Running && x.LastExecution < stuckBefore && !ignored.Contains(x.Id))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            var overdue = new List<string>();
            if (settings.OverdueAfter is { } overdueAfter && overdueAfter > TimeSpan.Zero) {
                var overdueBefore = now - overdueAfter;
                overdue = await dbContext.Tasks
                    .AsNoTracking()
                    .Where(x => x.Enabled != false && x.Status != ScheduledTaskStatus.Running && x.NextExecution < overdueBefore && !ignored.Contains(x.Id))
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);
            }
            var data = new Dictionary<string, object> { ["stuck"] = stuck, ["overdue"] = overdue };
            if (stuck.Count == 0 && overdue.Count == 0) {
                return HealthCheckResult.Healthy("No stuck scheduled tasks.", data);
            }
            var problems = new List<string>();
            if (stuck.Count > 0) {
                problems.Add($"{stuck.Count} task(s) running for more than {settings.StuckAfter}: {string.Join(", ", stuck)}");
            }
            if (overdue.Count > 0) {
                problems.Add($"{overdue.Count} task(s) overdue by more than {settings.OverdueAfter}: {string.Join(", ", overdue)}");
            }
            return new HealthCheckResult(context.Registration.FailureStatus, string.Join(". ", problems) + ".", data: data);
        } catch (Exception exception) when (exception is not OperationCanceledException) {
            return new HealthCheckResult(context.Registration.FailureStatus, "Could not read the scheduled tasks.", exception);
        }
    }
}
