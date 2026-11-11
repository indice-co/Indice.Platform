using Indice.Hosting.Data;
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
/// Each request is claimed with a delete, so with many workers only one of them runs the task.
/// The task runs through its normal Quartz job, so the singleton lock and the persisted state work as in a scheduled execution.
/// </remarks>
[DisallowConcurrentExecution]
internal class RunNowJob : IJob
{
    private const int BatchSize = 50;
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
            .Select(x => new { x.Id, x.QueueName, x.Date })
            .Take(BatchSize)
            .ToListAsync(context.CancellationToken);
        var triggered = new HashSet<string>(StringComparer.Ordinal);
        foreach (var request in requests) {
            // Claim the request. Another worker that hosts the same task may have claimed it first.
            var claimed = await _dbContext.Queue.Where(x => x.Id == request.Id).ExecuteDeleteAsync(context.CancellationToken) > 0;
            if (!claimed || !jobKeys.TryGetValue(request.QueueName, out var jobKey)) {
                continue;
            }
            if (DateTime.SpecifyKind(request.Date, DateTimeKind.Utc) < now - _settings.Expiration) {
                _logger.LogWarning("Run now request {RequestId} of scheduled task '{JobKey}' expired and was discarded.", request.Id, jobKey);
                continue;
            }
            // Requests made while the task was waiting to be picked up are served by a single run.
            if (!triggered.Add(request.QueueName)) {
                continue;
            }
            _logger.LogInformation("Running scheduled task '{JobKey}' now, as requested by {RequestId}.", jobKey, request.Id);
            await context.Scheduler.TriggerJob(jobKey, new JobDataMap { [JobDataKeys.RunNow] = true }, context.CancellationToken);
        }
    }
}
