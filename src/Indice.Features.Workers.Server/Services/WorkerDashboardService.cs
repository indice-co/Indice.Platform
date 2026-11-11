using System.Data;
using System.Text.Json;
using Indice.Features.Workers.Server.Services;
using Indice.Features.Workers.Server.Models;
using Indice.Hosting;
using Indice.Hosting.Data;
using Indice.Hosting.Data.Models;
using Indice.Hosting.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Indice.Features.Workers.Server.Services;

/// <summary>
/// Reads the worker tables through <see cref="ITaskDbContext"/> and <see cref="ILockDbContext"/>.
/// The dashboard runs in a host without <c>AddWorkerHost()</c>, so everything it shows comes from the database.
/// </summary>
/// <remarks>
/// The workers use the same tables in real time, so they come first: the aggregations and lists of the queue table run in <see cref="IsolationLevel.ReadUncommitted"/>.
/// They take no shared locks and never block a dequeue, at the cost of counters that may include messages of uncommitted transactions.
/// </remarks>
public class WorkerDashboardService : IWorkerDashboardService
{
    private const int PayloadPreviewLength = 200;
    private const int MaxPageSize = 500;
    private readonly ITaskDbContext _taskDb;
    private readonly ILockDbContext _lockDb;

    /// <summary>Creates a new instance of <see cref="WorkerDashboardService"/>.</summary>
    public WorkerDashboardService(ITaskDbContext taskDb, ILockDbContext lockDb) {
        _taskDb = taskDb ?? throw new ArgumentNullException(nameof(taskDb));
        _lockDb = lockDb ?? throw new ArgumentNullException(nameof(lockDb));
    }

    /// <inheritdoc />
    public async Task<DashboardStats> GetStats(CancellationToken cancellationToken = default) {
        var now = DateTime.UtcNow;
        var queues = await GetQueues(cancellationToken);
        var tasks = await GetScheduledTaskRows(cancellationToken);
        var activeLocks = await _lockDb.Locks.AsNoTracking().CountAsync(x => x.ExpirationDate >= now, cancellationToken);
        var taskInfos = tasks.Select(row => ToInfo(row, new ScheduledTaskInfo())).ToList();
        return new DashboardStats {
            ServerTimeUtc = now,
            Queues = queues.Count,
            Enqueued = queues.Sum(x => x.Enqueued),
            Scheduled = queues.Sum(x => x.Scheduled),
            Retrying = queues.Sum(x => x.Retrying),
            Poison = queues.Sum(x => x.Poison),
            Dequeued = queues.Sum(x => x.Dequeued),
            ScheduledTasks = taskInfos.Count,
            RunningTasks = taskInfos.Count(x => x.Status == ScheduledTaskState.Running),
            FailingTasks = taskInfos.Count(x => x.LastRunFailed),
            DisabledTasks = taskInfos.Count(x => !x.Enabled),
            ActiveLocks = activeLocks,
            Servers = taskInfos.Where(x => x.WorkerId is not null).Select(x => x.WorkerId).Distinct(StringComparer.Ordinal).Count()
        };
    }

    /// <inheritdoc />
    public Task<List<QueueInfo>> GetQueues(CancellationToken cancellationToken = default) => QueryQueues(null, cancellationToken);

    /// <inheritdoc />
    public async Task<QueueInfo> GetQueue(string queueName, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(queueName);
        var queues = await QueryQueues(queueName, cancellationToken);
        // A queue without messages has no rows at all, which reads as zero everywhere.
        return queues.FirstOrDefault() ?? new QueueInfo { Name = queueName };
    }

    private async Task<List<QueueInfo>> QueryQueues(string? queueName, CancellationToken cancellationToken) {
        var now = DateTime.UtcNow;
        var query = _taskDb.Queue.AsNoTracking();
        if (queueName is null) {
            query = query.Where(x => !x.QueueName.StartsWith(HostingConventions.RunNowQueuePrefix));
        } else {
            // Two index seeks instead of a scan of every queue.
            var poisonQueueName = queueName + HostingConventions.PoisonSuffix;
            query = query.Where(x => x.QueueName == queueName || x.QueueName == poisonQueueName);
        }
        // Covered by the (QueueName, Date) INCLUDE (State, DequeueCount) index, so the payloads are never read.
        var rows = await ReadUncommitted(_taskDb.Database, () => query
            .GroupBy(x => x.QueueName)
            .Select(g => new {
                QueueName = g.Key,
                Total = g.Count(),
                PoisonState = g.Sum(x => x.State == QMessageState.Poison ? 1 : 0),
                Dequeued = g.Sum(x => x.State == QMessageState.Dequeued ? 1 : 0),
                Scheduled = g.Sum(x => x.State == QMessageState.New && x.Date > now ? 1 : 0),
                Retrying = g.Sum(x => x.State == QMessageState.New && x.Date <= now && x.DequeueCount > 0 ? 1 : 0),
                Enqueued = g.Sum(x => x.State == QMessageState.New && x.Date <= now && x.DequeueCount == 0 ? 1 : 0),
                OldestEnqueuedAt = g.Min(x => x.State == QMessageState.New && x.Date <= now ? (DateTime?)x.Date : null)
            })
            .ToListAsync(cancellationToken), cancellationToken);
        var queues = new Dictionary<string, QueueInfo>(StringComparer.Ordinal);
        foreach (var row in rows) {
            var name = QueueMessageStatusRules.GetLogicalQueueName(row.QueueName);
            if (!queues.TryGetValue(name, out var queue)) {
                queues[name] = queue = new QueueInfo { Name = name };
            }
            queue.Total += row.Total;
            if (name != row.QueueName) {
                // Everything in the {name}-poison queue is poison, whatever its state.
                queue.Poison += row.Total;
                continue;
            }
            queue.Poison += row.PoisonState;
            queue.Dequeued += row.Dequeued;
            queue.Scheduled += row.Scheduled;
            queue.Retrying += row.Retrying;
            queue.Enqueued += row.Enqueued;
            queue.OldestEnqueuedAt = AsUtc(row.OldestEnqueuedAt);
        }
        return [.. queues.Values.OrderBy(x => x.Name, StringComparer.Ordinal)];
    }

    /// <inheritdoc />
    public async Task<ResultSet<QueueMessageListItem>> GetQueueMessages(string queueName, QueueMessageListOptions options, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(queueName);
        ArgumentNullException.ThrowIfNull(options);
        var now = DateTime.UtcNow;
        var poisonQueueName = queueName + HostingConventions.PoisonSuffix;
        var query = _taskDb.Queue
            .AsNoTracking()
            .Where(x => x.QueueName == queueName || x.QueueName == poisonQueueName)
            .WhereStatus(options.Status, now);
        if (!string.IsNullOrWhiteSpace(options.Search)) {
            var search = options.Search.Trim();
            query = Guid.TryParse(search, out var id)
                ? query.Where(x => x.Id == id)
                : query.Where(x => x.Payload.Contains(search));
        }
        // options.Sort is ignored: messages are always listed in the order they become visible.
        var page = await ReadUncommitted(_taskDb.Database, () => query
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Id)
            .Select(x => new {
                x.Id,
                x.QueueName,
                x.State,
                x.Date,
                x.DequeueCount,
                PayloadPreview = x.Payload.Length > PayloadPreviewLength ? x.Payload.Substring(0, PayloadPreviewLength) : x.Payload
            })
            .ToResultSetAsync(Math.Max(options.Page ?? 1, 1), Math.Clamp(options.Size ?? ListOptions.DEFAULT_SIZE, 0, MaxPageSize), cancellationToken), cancellationToken);
        var items = page.Items.Select(x => new QueueMessageListItem {
            Id = x.Id,
            QueueName = x.QueueName,
            Status = QueueMessageStatusRules.Classify(x.QueueName, x.State, x.Date, x.DequeueCount, now),
            Date = DateTime.SpecifyKind(x.Date, DateTimeKind.Utc),
            DequeueCount = x.DequeueCount,
            PayloadPreview = x.PayloadPreview
        });
        return new ResultSet<QueueMessageListItem>(items, page.Count);
    }

    /// <inheritdoc />
    public async Task<QueueMessageDetails?> GetMessage(Guid messageId, CancellationToken cancellationToken = default) {
        var message = await _taskDb.Queue.AsNoTracking().SingleOrDefaultAsync(x => x.Id == messageId, cancellationToken);
        return message is null ? null : ToDetails(message, DateTime.UtcNow);
    }

    private static QueueMessageDetails ToDetails(DbQMessage message, DateTime now) => new() {
        Id = message.Id,
        QueueName = message.QueueName,
        LogicalQueueName = QueueMessageStatusRules.GetLogicalQueueName(message.QueueName),
        Status = QueueMessageStatusRules.Classify(message.QueueName, message.State, message.Date, message.DequeueCount, now),
        Date = DateTime.SpecifyKind(message.Date, DateTimeKind.Utc),
        DequeueCount = message.DequeueCount,
        PayloadPreview = message.Payload?.Length > PayloadPreviewLength ? message.Payload[..PayloadPreviewLength] : message.Payload,
        Payload = message.Payload
    };

    /// <inheritdoc />
    public async Task<int> RequeueMessages(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0) {
            return 0;
        }

        return await _taskDb.Queue
            .Where(x => ids.Contains(x.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.QueueName, x =>
                    x.QueueName.EndsWith(HostingConventions.PoisonSuffix)
                        ? x.QueueName.Substring(0, x.QueueName.Length - HostingConventions.PoisonSuffix.Length)
                        : x.QueueName
                )
                .SetProperty(x => x.State, QMessageState.New)
                .SetProperty(x => x.DequeueCount, 0)
                .SetProperty(x => x.Date, DateTime.UtcNow), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteMessages(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0) {
            return 0;
        }
        
        return await _taskDb.Queue
            .Where(x => ids.Contains(x.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<QueueMessageDetails> EnqueueMessage(string queueName, string payload, DateTime? visibleAt, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        var now = DateTime.UtcNow;
        // A date without an offset is taken as UTC, like every date of the API.
        var visibleAtUtc = visibleAt is { Kind: DateTimeKind.Unspecified } ? DateTime.SpecifyKind(visibleAt.Value, DateTimeKind.Utc) : visibleAt?.ToUniversalTime();
        var message = new DbQMessage {
            Id = Guid.NewGuid(),
            QueueName = queueName,
            Payload = payload,
            Date = visibleAtUtc > now ? visibleAtUtc.Value : now,
            DequeueCount = 0,
            State = QMessageState.New
        };
        _taskDb.Add(message);
        await _taskDb.SaveChangesAsync(cancellationToken);
        return ToDetails(message, now);
    }

    /// <inheritdoc />
    public async Task<QueueMessageDetails?> UpdateMessage(Guid messageId, string payload, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        // A message that a worker has dequeued is gone (deleted), so this cannot overwrite a message in progress.
        var affected = await _taskDb.Queue
            .Where(x => x.Id == messageId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Payload, payload)
                .SetProperty(x => x.QueueName, x =>
                    x.QueueName.EndsWith(HostingConventions.PoisonSuffix)
                        ? x.QueueName.Substring(0, x.QueueName.Length - HostingConventions.PoisonSuffix.Length)
                        : x.QueueName
                )
                .SetProperty(x => x.State, QMessageState.New)
                .SetProperty(x => x.DequeueCount, 0)
                .SetProperty(x => x.Date, DateTime.UtcNow), cancellationToken);
        return affected > 0 ? await GetMessage(messageId, cancellationToken) : null;
    }

    /// <inheritdoc />
    public async Task<List<ScheduledTaskInfo>> GetScheduledTasks(CancellationToken cancellationToken = default) {
        var rows = await GetScheduledTaskRows(cancellationToken);
        // A range seek on the (QueueName, Date) index: the run now queues share a prefix.
        var runRequests = await _taskDb.Queue
            .AsNoTracking()
            .Where(x => x.QueueName.StartsWith(HostingConventions.RunNowQueuePrefix))
            .GroupBy(x => x.QueueName)
            .Select(g => new { QueueName = g.Key, RequestedAt = g.Min(x => x.Date) })
            .ToDictionaryAsync(x => x.QueueName, x => x.RequestedAt, StringComparer.Ordinal, cancellationToken);
        return [
            .. rows.Select(row => {
                    var info = ToInfo(row, new ScheduledTaskInfo());
                    info.RunRequestedAt = runRequests.TryGetValue(HostingConventions.GetRunNowQueueName(row.Id), out var requestedAt) ? AsUtc(requestedAt) : null;
                    return info;
                })
                .OrderBy(x => x.Group, StringComparer.Ordinal)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
        ];
    }

    private Task<List<ScheduledTaskRow>> GetScheduledTaskRows(CancellationToken cancellationToken) =>
        // Payload and Errors can be large, so the list leaves them out.
        _taskDb.Tasks
            .AsNoTracking()
            .Select(x => new ScheduledTaskRow {
                Id = x.Id,
                WorkerId = x.WorkerId,
                Group = x.Group,
                Description = x.Description,
                Type = x.Type,
                LastExecution = x.LastExecution,
                NextExecution = x.NextExecution,
                ExecutionCount = x.ExecutionCount,
                Status = x.Status,
                LastErrorDate = x.LastErrorDate,
                HasErrors = x.Errors != null,
                Progress = x.Progress,
                Enabled = x.Enabled
            })
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<ScheduledTaskDetails?> GetScheduledTask(string taskId, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(taskId);
        
        var task = await _taskDb.Tasks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null) {
            return null;
        }
        
        var row = new ScheduledTaskRow {
            Id = task.Id,
            WorkerId = task.WorkerId,
            Group = task.Group,
            Description = task.Description,
            Type = task.Type,
            LastExecution = task.LastExecution,
            NextExecution = task.NextExecution,
            ExecutionCount = task.ExecutionCount,
            Status = task.Status,
            LastErrorDate = task.LastErrorDate,
            HasErrors = task.Errors != null,
            Progress = task.Progress,
            Enabled = task.Enabled
        };
        
        var details = ToInfo(row, new ScheduledTaskDetails());
        details.Errors = task.Errors;
        details.State = task.Payload;
        var runNowQueueName = HostingConventions.GetRunNowQueueName(taskId);
        details.RunRequestedAt = AsUtc(await _taskDb.Queue.AsNoTracking().Where(x => x.QueueName == runNowQueueName).MinAsync(x => (DateTime?)x.Date, cancellationToken));

        return details;
    }

    /// <inheritdoc />
    public async Task<bool> SetScheduledTaskEnabled(string taskId, bool enabled, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(taskId);
        // Safe while a worker runs the task: DbScheduledTask.From() never overwrites Enabled.
        var affected = await _taskDb.Tasks
            .Where(x => x.Id == taskId)
            .ExecuteUpdateAsync(s => 
                s.SetProperty(x => x.Enabled, enabled), cancellationToken);
        
        return affected > 0;
    }

    /// <inheritdoc />
    public async Task<RunScheduledTaskResult?> RunScheduledTask(string taskId, string? requestedBy = null, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(taskId);
        if (!await _taskDb.Tasks.AnyAsync(x => x.Id == taskId, cancellationToken)) {
            return null;
        }
        var queueName = HostingConventions.GetRunNowQueueName(taskId);
        var pending = await _taskDb.Queue
            .AsNoTracking()
            .Where(x => x.QueueName == queueName)
            .OrderBy(x => x.Date)
            .Select(x => new { x.Id, x.Date })
            .FirstOrDefaultAsync(cancellationToken);
        if (pending is not null) {
            return new RunScheduledTaskResult { RequestId = pending.Id, RequestedAt = DateTime.SpecifyKind(pending.Date, DateTimeKind.Utc) };
        }
        var now = DateTime.UtcNow;
        var request = new DbQMessage {
            Id = Guid.NewGuid(),
            QueueName = queueName,
            // The worker only needs the queue name; the payload is kept for troubleshooting.
            Payload = JsonSerializer.Serialize(new { taskId, requestedBy, requestedAt = now }),
            Date = now,
            DequeueCount = 0,
            State = QMessageState.New
        };
        _taskDb.Add(request);
        await _taskDb.SaveChangesAsync(cancellationToken);
        return new RunScheduledTaskResult { RequestId = request.Id, RequestedAt = now };
    }

    /// <inheritdoc />
    public async Task<List<LockInfo>> GetLocks(CancellationToken cancellationToken = default) {
        var now = DateTime.UtcNow;
        var locks = await _lockDb.Locks.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        // Renewals use the database server time (GETDATE()); it is treated as UTC here.
        return [
            .. locks.Select(x => new LockInfo {
                Id = x.Id,
                Name = x.Name,
                ExpirationDate = DateTime.SpecifyKind(x.ExpirationDate, DateTimeKind.Utc),
                DurationSeconds = x.Duration,
                IsExpired = x.ExpirationDate < now
            })
        ];
    }

    /// <inheritdoc />
    public async Task<bool> ReleaseLock(Guid lockId, CancellationToken cancellationToken = default) =>
        await _lockDb.Locks.Where(x => x.Id == lockId).ExecuteDeleteAsync(cancellationToken) > 0;

    /// <inheritdoc />
    public async Task<List<WorkerServerInfo>> GetServers(CancellationToken cancellationToken = default) {
        var servers = await _taskDb.Tasks
            .AsNoTracking()
            .Where(x => x.WorkerId != null)
            .GroupBy(x => x.WorkerId)
            .Select(g => new WorkerServerInfo {
                WorkerId = g.Key!,
                LastSeen = g.Max(x => (DateTimeOffset?)x.LastExecution),
                Tasks = g.Count(),
                RunningTasks = g.Sum(x => x.Status == ScheduledTaskStatus.Running ? 1 : 0)
            })
            .ToListAsync(cancellationToken);
        
        return [.. servers.OrderByDescending(x => x.LastSeen)];
    }

    private static TInfo ToInfo<TInfo>(ScheduledTaskRow row, TInfo info) where TInfo : ScheduledTaskInfo {
        info.Id = row.Id;
        info.Name = GetTaskName(row.Id, row.Group);
        info.Group = row.Group;
        info.Description = row.Description;
        info.JobType = GetHandlerTypeName(row.Type);
        info.Status = row.Status == ScheduledTaskStatus.Running ? ScheduledTaskState.Running : ScheduledTaskState.Idle;
        info.Enabled = row.Enabled ?? true;
        info.HasRun = true;
        info.LastExecution = row.LastExecution;
        info.NextExecution = row.NextExecution;
        info.ExecutionCount = row.ExecutionCount;
        info.LastErrorDate = row.LastErrorDate;
        // Legacy rows can have Errors but no LastErrorDate because older workers did not persist it.
        info.LastRunFailed = row.LastErrorDate is not null
            ? row.LastErrorDate >= row.LastExecution
            : row.HasErrors;
        info.Progress = row.Progress;
        info.WorkerId = row.WorkerId;
        return info;
    }

    /// <summary>The id is the Quartz job key, <c>{group}.{name}</c>.</summary>
    private static string GetTaskName(string id, string? group) {
        var prefix = $"{group}.";
        return !string.IsNullOrEmpty(group) && id.StartsWith(prefix, StringComparison.Ordinal) && id.Length > prefix.Length ? id[prefix.Length..] : id;
    }

    /// <summary>
    /// The stored type is the Quartz job, e.g. <c>Indice.Hosting.Tasks.ScheduledJob`2[MyApp.CleanupHandler,MyApp.CleanupState]</c>.
    /// Returns its first generic argument (the handler), or the stored type when it does not have that shape.
    /// </summary>
    private static string? GetHandlerTypeName(string? type) {
        if (string.IsNullOrEmpty(type)) {
            return type;
        }
        var start = type.IndexOf('[');
        if (start < 0 || !type.EndsWith(']')) {
            return type;
        }
        var depth = 0;
        for (var i = start + 1; i < type.Length - 1; i++) {
            switch (type[i]) {
                case '[': depth++; break;
                case ']': depth--; break;
                case ',' when depth == 0:
                    var handler = type[(start + 1)..i].Trim();
                    return handler.Length > 0 ? handler : type;
            }
        }
        return type;
    }

    private static DateTime? AsUtc(DateTime? date) => date.HasValue ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc) : null;

    /// <summary>Runs a read in <see cref="IsolationLevel.ReadUncommitted"/>, so that it takes no shared locks on the rows the workers dequeue.</summary>
    internal static async Task<T> ReadUncommitted<T>(DatabaseFacade database, Func<Task<T>> query, CancellationToken cancellationToken) {
        if (!database.IsRelational() || database.CurrentTransaction is not null) {
            return await query();
        }
        // A retrying execution strategy only allows a user initiated transaction inside the strategy.
        return await database.CreateExecutionStrategy().ExecuteAsync(async () => {
            await using var transaction = await database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, cancellationToken);
            var result = await query();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    private sealed class ScheduledTaskRow
    {
        public string Id { get; set; } = null!;
        public string? WorkerId { get; set; }
        public string? Group { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public DateTimeOffset LastExecution { get; set; }
        public DateTimeOffset? NextExecution { get; set; }
        public int ExecutionCount { get; set; }
        public ScheduledTaskStatus Status { get; set; }
        public DateTimeOffset? LastErrorDate { get; set; }
        public bool HasErrors { get; set; }
        public double Progress { get; set; }
        public bool? Enabled { get; set; }
    }
}
