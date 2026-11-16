using Indice.Features.Workers.Server.Services;
using Indice.Hosting;
using Indice.Hosting.Data;
using Indice.Hosting.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Indice.Features.Workers.Server.HealthChecks;

/// <summary>Options of the <see cref="WorkerPoisonMessagesHealthCheck"/>.</summary>
public class WorkerPoisonMessagesHealthCheckOptions
{
    /// <summary>The number of poison messages that is still healthy. Above it the check reports its failure status. Defaults to 0.</summary>
    public int Threshold { get; set; }
    /// <summary>Limits the check to these logical queues (without the <c>-poison</c> suffix). Empty checks every queue.</summary>
    public ICollection<string> QueueNames { get; set; } = [];
}

/// <summary>Reports the poison messages of the worker queues when they are more than <see cref="WorkerPoisonMessagesHealthCheckOptions.Threshold"/>.</summary>
/// <param name="dbContext">The worker tables.</param>
/// <param name="options">The options, per health check registration name.</param>
public class WorkerPoisonMessagesHealthCheck(ITaskDbContext dbContext, IOptionsMonitor<WorkerPoisonMessagesHealthCheckOptions> options) : IHealthCheck
{
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        var settings = options.Get(context.Registration.Name);
        var poisonSuffix = HostingConventions.PoisonSuffix;
        var runNowPrefix = HostingConventions.RunNowQueuePrefix;
        var query = dbContext.Queue.Where(x => (x.QueueName.EndsWith(poisonSuffix) || x.State == QMessageState.Poison) && !x.QueueName.StartsWith(runNowPrefix));
        if (settings.QueueNames.Count > 0) {
            var queueNames = settings.QueueNames.SelectMany(name => new[] { name, name + poisonSuffix }).ToList();
            query = query.Where(x => queueNames.Contains(x.QueueName));
        }
        try {
            var rows = await WorkerDashboardService.ReadUncommitted(dbContext.Database, () => query
                .GroupBy(x => x.QueueName)
                .Select(g => new { QueueName = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken), cancellationToken);
            var perQueue = rows
                .GroupBy(x => x.QueueName.EndsWith(poisonSuffix, StringComparison.Ordinal) ? x.QueueName[..^poisonSuffix.Length] : x.QueueName)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => (object)g.Sum(x => x.Count));
            var total = rows.Sum(x => x.Count);
            var data = new Dictionary<string, object>(perQueue) { ["total"] = total, ["threshold"] = settings.Threshold };
            if (total > settings.Threshold) {
                return new HealthCheckResult(context.Registration.FailureStatus, $"{total} poison message(s), above the threshold of {settings.Threshold}.", data: data);
            }
            return HealthCheckResult.Healthy($"{total} poison message(s).", data);
        } catch (Exception exception) when (exception is not OperationCanceledException) {
            return new HealthCheckResult(context.Registration.FailureStatus, "Could not count the poison messages.", exception);
        }
    }
}
