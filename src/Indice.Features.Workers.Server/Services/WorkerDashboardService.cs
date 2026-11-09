using Indice.Features.Workers.Server.Services;
using Indice.Features.Workers.Server.Models;
using Indice.Hosting;
using Indice.Hosting.Data;
using Indice.Hosting.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Workers.Server.Services;

/// <summary>
/// Reads the worker tables through <see cref="ITaskDbContext"/> and <see cref="ILockDbContext"/>.
/// The dashboard runs in a host without <c>AddWorkerHost()</c>, so everything it shows comes from the database.
/// </summary>
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
        var queues = await GetQueues(cancellationToken);
        var tasks = await GetScheduledTasks(cancellationToken);
        var locks = await GetLocks(cancellationToken);
        var servers = await GetServers(cancellationToken);
        return new DashboardStats {
            ServerTimeUtc = DateTime.UtcNow,
            Queues = queues.Count,
            Enqueued = queues.Sum(x => x.Enqueued),
            Scheduled = queues.Sum(x => x.Scheduled),
            Retrying = queues.Sum(x => x.Retrying),
            Poison = queues.Sum(x => x.Poison),
            Dequeued = queues.Sum(x => x.Dequeued),
            ScheduledTasks = tasks.Count,
            RunningTasks = tasks.Count(x => x.Status == ScheduledTaskState.Running),
            FailingTasks = tasks.Count(x => x.LastRunFailed),
            DisabledTasks = tasks.Count(x => !x.Enabled),
            ActiveLocks = locks.Count(x => !x.IsExpired),
            Servers = servers.Count
        };
    }

    /// <inheritdoc />
    public async Task<List<QueueInfo>> GetQueues(CancellationToken cancellationToken = default) {
        var now = DateTime.UtcNow;
        var rows = await _taskDb.Queue
            .AsNoTracking()
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
            .ToListAsync(cancellationToken);
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
        var page = await query
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
            .ToResultSetAsync(Math.Max(options.Page ?? 1, 1), Math.Clamp(options.Size ?? ListOptions.DEFAULT_SIZE, 0, MaxPageSize), cancellationToken);
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
        var now = DateTime.UtcNow;
        var message = await _taskDb.Queue.AsNoTracking().SingleOrDefaultAsync(x => x.Id == messageId, cancellationToken);
        if (message is null) {
            return null;
        }
        return new QueueMessageDetails {
            Id = message.Id,
            QueueName = message.QueueName,
            LogicalQueueName = QueueMessageStatusRules.GetLogicalQueueName(message.QueueName),
            Status = QueueMessageStatusRules.Classify(message.QueueName, message.State, message.Date, message.DequeueCount, now),
            Date = DateTime.SpecifyKind(message.Date, DateTimeKind.Utc),
            DequeueCount = message.DequeueCount,
            PayloadPreview = message.Payload?.Length > PayloadPreviewLength ? message.Payload[..PayloadPreviewLength] : message.Payload,
            Payload = message.Payload
        };
    }

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
    public async Task<List<ScheduledTaskInfo>> GetScheduledTasks(CancellationToken cancellationToken = default) {
        // Payload and Errors can be large, so the list leaves them out.
        var rows = await _taskDb.Tasks
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
        return [
            .. rows.Select(row => ToInfo(row, new ScheduledTaskInfo()))
                .OrderBy(x => x.Group, StringComparer.Ordinal)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
        ];
    }

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
