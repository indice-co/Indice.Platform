using Indice.Hosting.Data;
using Indice.Hosting.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Indice.Hosting.Tasks;

/// <summary>Settings of the internal job that picks up "run now" requests of scheduled tasks.</summary>
internal sealed record RunNowSettings(TimeSpan PollingInterval, TimeSpan Expiration);

/// <summary>
/// Picks up the "run now" requests of the scheduled tasks hosted by this worker and fires their Quartz job once.
/// A request is a message in the <c>$run-now:{taskId}</c> queue (see <see cref="HostingConventions.GetRunNowQueueName(string)"/>).
/// </summary>
/// <remarks>
/// Each request is claimed with a short database lease, so with many workers only one of them dispatches it.
/// A failed dispatch releases the request, and a crashed worker's claim expires so the request can be retried.
/// The task runs through its normal Quartz job, so the singleton lock and the persisted state work as in a scheduled execution.
/// </remarks>
[DisallowConcurrentExecution]
internal class RunNowJob : IJob
{
    private const int BatchSize = 50;
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromMinutes(1);
    private readonly ITaskDbContext _dbContext;
    private readonly RunNowSettings _settings;
    private readonly ILogger<RunNowJob> _logger;

    public RunNowJob(ITaskDbContext dbContext, RunNowSettings settings, ILogger<RunNowJob> logger) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Execute(IJobExecutionContext context) {
        var jobKeys = context.JobDetail.JobDataMap[JobDataKeys.RunNow] as IReadOnlyDictionary<string, JobKey>;
        if (jobKeys is null || jobKeys.Count == 0) {
            return;
        }
        try {
            await ProcessRequests(context, jobKeys);
        } catch (Exception exception) {
            _logger.LogError(exception, "An error occurred while checking for run now requests.");
        }
    }

    private async Task ProcessRequests(IJobExecutionContext context, IReadOnlyDictionary<string, JobKey> jobKeys) {
        var now = DateTime.UtcNow;
        var queueNames = jobKeys.Keys.ToArray();
        // Served by the (QueueName, Date) index: an equality seek per run now queue of this worker.
        var requests = await _dbContext.Queue
            .AsNoTracking()
            .Where(x => queueNames.Contains(x.QueueName) && x.Date <= now)
            .OrderBy(x => x.Date)
            .Select(x => new { x.Id, x.QueueName, x.Date, x.Payload })
            .Take(BatchSize)
            .ToListAsync(context.CancellationToken);
        var triggered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var request in requests) {
            now = DateTime.UtcNow;
            var requestedAt = GetRequestedAt(request.Payload, request.Date);
            // Claim until a short lease expires. A crashed worker then leaves a request another worker can retry.
            var leaseUntil = now + ClaimDuration;
            var claimed = await _dbContext.Queue
                .Where(x => x.Id == request.Id && x.Date <= now)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.State, QMessageState.Dequeued)
                    .SetProperty(x => x.Date, leaseUntil), context.CancellationToken) > 0;
            if (!claimed || !jobKeys.TryGetValue(request.QueueName, out var jobKey)) {
                continue;
            }
            if (requestedAt < now - _settings.Expiration) {
                _logger.LogWarning("Run now request {RequestId} of scheduled task '{JobKey}' expired and was discarded.", request.Id, jobKey);
                await DeleteRequest(request.Id);
                continue;
            }
            // Requests made while the task was waiting to be picked up are served by a single run.
            if (!triggered.Add(request.QueueName)) {
                await DeleteRequest(request.Id);
                continue;
            }
            _logger.LogInformation("Running scheduled task '{JobKey}' now, as requested by {RequestId}.", jobKey, request.Id);
            var dispatched = false;
            try {
                await context.Scheduler.TriggerJob(jobKey, new JobDataMap { [JobDataKeys.RunNow] = true }, context.CancellationToken);
                dispatched = true;
            } finally {
                if (dispatched) {
                    await DeleteRequest(request.Id);
                } else {
                    await ReleaseRequest(request.Id, requestedAt);
                }
            }
        }
    }

    private static DateTime GetRequestedAt(string payload, DateTime fallback) {
        try {
            using var document = System.Text.Json.JsonDocument.Parse(payload);
            return document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object &&
                document.RootElement.TryGetProperty("requestedAt", out var value) &&
                value.TryGetDateTime(out var requestedAt)
                ? DateTime.SpecifyKind(requestedAt, DateTimeKind.Utc)
                : DateTime.SpecifyKind(fallback, DateTimeKind.Utc);
        } catch (System.Text.Json.JsonException) {
            return DateTime.SpecifyKind(fallback, DateTimeKind.Utc);
        }
    }

    private Task DeleteRequest(Guid id) =>
        _dbContext.Queue.Where(x => x.Id == id).ExecuteDeleteAsync(CancellationToken.None);

    private Task<int> ReleaseRequest(Guid id, DateTime requestedAt) =>
        _dbContext.Queue.Where(x => x.Id == id).ExecuteUpdateAsync(s => s
            .SetProperty(x => x.State, QMessageState.New)
            .SetProperty(x => x.Date, requestedAt), CancellationToken.None);
}
