using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Indice.Hosting.Data.Models;

namespace Indice.Hosting.Diagnostics;

/// <summary>How the processing of a queue message ended.</summary>
internal enum MessageOutcome
{
    /// <summary>The handler succeeded.</summary>
    Succeeded,
    /// <summary>The handler failed and the message goes back to the queue.</summary>
    Retried,
    /// <summary>The handler failed for the last time and the message becomes poison.</summary>
    Poisoned
}

/// <summary>
/// Measures the work of the current process. It publishes the instruments of the <see cref="WorkerDiagnostics.MeterName"/> meter for OpenTelemetry and <c>dotnet-counters</c>,
/// and keeps the totals per queue and task that the server heartbeat stores, so that the dashboard can show the throughput of the cluster.
/// </summary>
internal sealed class WorkerMetrics : IDisposable
{
    private const string QueueTag = "worker.queue";
    private const string TaskTag = "worker.task";
    private const string OutcomeTag = "worker.outcome";
#if NET9_0_OR_GREATER
    // The default buckets suit milliseconds; these suit seconds, from a quick handler to a long scheduled task.
    private static readonly InstrumentAdvice<double> DurationAdvice = new() {
        HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10, 30, 60, 120, 300, 600, 1800, 3600]
    };
#endif
    private readonly Meter _meter;
    private readonly bool _ownsMeter;
    private readonly TimeProvider _timeProvider;
    private readonly Counter<long> _messagesProcessed;
    private readonly UpDownCounter<long> _messagesActive;
    private readonly Histogram<double> _messageDuration;
    private readonly Histogram<double> _messageWaitTime;
    private readonly Counter<long> _taskRuns;
    private readonly UpDownCounter<long> _taskRunsActive;
    private readonly Histogram<double> _taskRunDuration;
    private readonly ConcurrentDictionary<string, QueueTotals> _queues = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TaskTotals> _tasks = new(StringComparer.Ordinal);
    // Past totals, oldest first, to compute the activity of the last window. The first one is the empty totals of startup.
    private readonly List<Sample> _samples = [];
    private readonly object _samplesLock = new();

    public WorkerMetrics(IMeterFactory? meterFactory = null, TimeProvider? timeProvider = null) {
        _timeProvider = timeProvider ?? TimeProvider.System;
        var version = typeof(WorkerMetrics).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (meterFactory is not null) {
            _meter = meterFactory.Create(new MeterOptions(WorkerDiagnostics.MeterName) { Version = version });
        } else {
            _meter = new Meter(WorkerDiagnostics.MeterName, version);
            _ownsMeter = true;
        }
        _messagesProcessed = _meter.CreateCounter<long>("indice.worker.queue.messages.processed", "{message}", "The number of queue messages processed, by queue and outcome (succeeded, retried, poisoned).");
        _messagesActive = _meter.CreateUpDownCounter<long>("indice.worker.queue.messages.active", "{message}", "The number of queue messages being processed, by queue.");
        _messageDuration = CreateDurationHistogram("indice.worker.queue.message.duration", "The time the handler took to process a queue message, by queue and outcome.");
        _messageWaitTime = CreateDurationHistogram("indice.worker.queue.message.wait_time", "The time a queue message waited in the queue before it was dequeued, by queue.");
        _taskRuns = _meter.CreateCounter<long>("indice.worker.task.runs", "{run}", "The number of scheduled task runs, by task and outcome (succeeded, failed).");
        _taskRunsActive = _meter.CreateUpDownCounter<long>("indice.worker.task.runs.active", "{run}", "The number of scheduled tasks running, by task.");
        _taskRunDuration = CreateDurationHistogram("indice.worker.task.run.duration", "The time a scheduled task run took, by task and outcome.");
        _samples.Add(new Sample(_timeProvider.GetUtcNow(), [], []));
    }

    /// <summary>Records that a queue message was dequeued and its handler starts.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="visibleAt">When the message became visible in the queue, in UTC.</param>
    /// <returns>The timestamp to pass to <see cref="MessageCompleted"/>.</returns>
    public long MessageStarted(string queue, DateTime visibleAt) {
        var wait = Math.Max(0, (_timeProvider.GetUtcNow().UtcDateTime - visibleAt).TotalSeconds);
        var totals = _queues.GetOrAdd(queue, static _ => new QueueTotals());
        lock (totals) {
            totals.Active++;
            totals.WaitCount++;
            totals.WaitSum += wait;
        }
        var tags = new TagList { { QueueTag, queue } };
        _messagesActive.Add(1, tags);
        _messageWaitTime.Record(wait, tags);
        return _timeProvider.GetTimestamp();
    }

    /// <summary>Records that the handler of a queue message ended.</summary>
    /// <param name="queue">The queue name.</param>
    /// <param name="startTimestamp">The timestamp returned by <see cref="MessageStarted"/>.</param>
    /// <param name="outcome">How the processing ended.</param>
    public void MessageCompleted(string queue, long startTimestamp, MessageOutcome outcome) {
        var duration = _timeProvider.GetElapsedTime(startTimestamp).TotalSeconds;
        var totals = _queues.GetOrAdd(queue, static _ => new QueueTotals());
        lock (totals) {
            totals.Active--;
            totals.DurationSum += duration;
            switch (outcome) {
                case MessageOutcome.Succeeded: totals.Succeeded++; break;
                case MessageOutcome.Retried: totals.Retried++; break;
                default: totals.Poisoned++; break;
            }
        }
        _messagesActive.Add(-1, new TagList { { QueueTag, queue } });
        var tags = new TagList { { QueueTag, queue }, { OutcomeTag, ToTagValue(outcome) } };
        _messagesProcessed.Add(1, tags);
        _messageDuration.Record(duration, tags);
    }

    /// <summary>Records that a scheduled task run starts.</summary>
    /// <param name="taskId">The task id.</param>
    /// <returns>The timestamp to pass to <see cref="TaskCompleted"/>.</returns>
    public long TaskStarted(string taskId) {
        var totals = _tasks.GetOrAdd(taskId, static _ => new TaskTotals());
        lock (totals) {
            totals.Active++;
        }
        _taskRunsActive.Add(1, new TagList { { TaskTag, taskId } });
        return _timeProvider.GetTimestamp();
    }

    /// <summary>Records that a scheduled task run ended.</summary>
    /// <param name="taskId">The task id.</param>
    /// <param name="startTimestamp">The timestamp returned by <see cref="TaskStarted"/>.</param>
    /// <param name="failed">Whether the run failed.</param>
    public void TaskCompleted(string taskId, long startTimestamp, bool failed) {
        var duration = _timeProvider.GetElapsedTime(startTimestamp).TotalSeconds;
        var totals = _tasks.GetOrAdd(taskId, static _ => new TaskTotals());
        lock (totals) {
            totals.Active--;
            totals.DurationSum += duration;
            totals.LastDuration = duration;
            if (failed) {
                totals.Failed++;
            } else {
                totals.Succeeded++;
            }
        }
        _taskRunsActive.Add(-1, new TagList { { TaskTag, taskId } });
        var tags = new TagList { { TaskTag, taskId }, { OutcomeTag, failed ? "failed" : "succeeded" } };
        _taskRuns.Add(1, tags);
        _taskRunDuration.Record(duration, tags);
    }

    /// <summary>Takes a snapshot of the work of the process: the totals since startup and the activity of the last <paramref name="window"/>.</summary>
    /// <param name="window">The period of the recent activity. Shorter while the process is younger than that.</param>
    public DbServerMetrics Collect(TimeSpan window) {
        lock (_samplesLock) {
            var now = _timeProvider.GetUtcNow();
            var current = new Sample(
                now,
                _queues.ToDictionary(x => x.Key, x => x.Value.Snapshot(), StringComparer.Ordinal),
                _tasks.ToDictionary(x => x.Key, x => x.Value.Snapshot(), StringComparer.Ordinal)
            );
            // The baseline is the latest sample that is at least a window old. Older samples are no longer needed.
            var baselineIndex = _samples.FindLastIndex(x => x.At <= now - window);
            if (baselineIndex > 0) {
                _samples.RemoveRange(0, baselineIndex);
            }
            var baseline = _samples[0];
            _samples.Add(current);
            return new DbServerMetrics {
                CollectedAt = now.UtcDateTime,
                Window = Math.Round((now - baseline.At).TotalSeconds, 1),
                Queues = current.Queues
                    .OrderBy(x => x.Key, StringComparer.Ordinal)
                    .Select(x => ToQueueMetrics(x.Key, x.Value, baseline.Queues.GetValueOrDefault(x.Key)))
                    .ToList(),
                Tasks = current.Tasks
                    .OrderBy(x => x.Key, StringComparer.Ordinal)
                    .Select(x => ToTaskMetrics(x.Key, x.Value))
                    .ToList()
            };
        }
    }

    public void Dispose() {
        if (_ownsMeter) {
            _meter.Dispose();
        }
    }

    private Histogram<double> CreateDurationHistogram(string name, string description) =>
#if NET9_0_OR_GREATER
        _meter.CreateHistogram(name, "s", description, tags: null, advice: DurationAdvice);
#else
        _meter.CreateHistogram<double>(name, "s", description);
#endif

    private static DbServerQueueMetrics ToQueueMetrics(string name, QueueSnapshot current, QueueSnapshot baseline) {
        var processed = current.Processed - baseline.Processed;
        var waited = current.WaitCount - baseline.WaitCount;
        return new DbServerQueueMetrics {
            Name = name,
            Processing = current.Active,
            Succeeded = current.Succeeded,
            Retried = current.Retried,
            Poisoned = current.Poisoned,
            RecentProcessed = processed,
            RecentFailed = current.Failed - baseline.Failed,
            RecentAverageDuration = processed > 0 ? ToMilliseconds((current.DurationSum - baseline.DurationSum) / processed) : null,
            RecentAverageWait = waited > 0 ? ToMilliseconds((current.WaitSum - baseline.WaitSum) / waited) : null
        };
    }

    private static DbServerTaskMetrics ToTaskMetrics(string id, TaskSnapshot current) {
        var runs = current.Succeeded + current.Failed;
        return new DbServerTaskMetrics {
            Id = id,
            Running = current.Active,
            Succeeded = current.Succeeded,
            Failed = current.Failed,
            AverageDuration = runs > 0 ? ToMilliseconds(current.DurationSum / runs) : null,
            LastDuration = current.LastDuration is { } last ? ToMilliseconds(last) : null
        };
    }

    private static double ToMilliseconds(double seconds) => Math.Round(seconds * 1000, 1);

    private static string ToTagValue(MessageOutcome outcome) => outcome switch {
        MessageOutcome.Succeeded => "succeeded",
        MessageOutcome.Retried => "retried",
        _ => "poisoned"
    };

    private sealed record Sample(DateTimeOffset At, Dictionary<string, QueueSnapshot> Queues, Dictionary<string, TaskSnapshot> Tasks);

    private readonly record struct QueueSnapshot(long Active, long Succeeded, long Retried, long Poisoned, double DurationSum, long WaitCount, double WaitSum)
    {
        public long Failed => Retried + Poisoned;
        public long Processed => Succeeded + Retried + Poisoned;
    }

    private readonly record struct TaskSnapshot(long Active, long Succeeded, long Failed, double DurationSum, double? LastDuration);

    private sealed class QueueTotals
    {
        public long Active;
        public long Succeeded;
        public long Retried;
        public long Poisoned;
        public double DurationSum;
        public long WaitCount;
        public double WaitSum;

        public QueueSnapshot Snapshot() {
            lock (this) {
                return new QueueSnapshot(Active, Succeeded, Retried, Poisoned, DurationSum, WaitCount, WaitSum);
            }
        }
    }

    private sealed class TaskTotals
    {
        public long Active;
        public long Succeeded;
        public long Failed;
        public double DurationSum;
        public double? LastDuration;

        public TaskSnapshot Snapshot() {
            lock (this) {
                return new TaskSnapshot(Active, Succeeded, Failed, DurationSum, LastDuration);
            }
        }
    }
}
