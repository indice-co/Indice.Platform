using System.Diagnostics.Metrics;
using Indice.Hosting.Diagnostics;

namespace Indice.Hosting.Tests;

public class WorkerMetricsTests
{
    private const string Queue = "metrics-queue";
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly ManualTimeProvider _time = new();

    [Fact]
    public void CollectReportsTheTotalsAndTheRecentActivity() {
        using var metrics = new WorkerMetrics(timeProvider: _time);
        _time.Advance(TimeSpan.FromSeconds(30));
        Process(metrics, wait: TimeSpan.FromSeconds(2), duration: TimeSpan.FromMilliseconds(100), MessageOutcome.Succeeded);
        Process(metrics, wait: TimeSpan.FromSeconds(4), duration: TimeSpan.FromMilliseconds(300), MessageOutcome.Retried);
        Process(metrics, wait: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(200), MessageOutcome.Poisoned);

        var snapshot = metrics.Collect(Window);

        // The process is younger than the window, so the window is its age.
        Assert.InRange(snapshot.Window, 30, 31);
        var queue = Assert.Single(snapshot.Queues);
        Assert.Equal(Queue, queue.Name);
        Assert.Equal(1, queue.Succeeded);
        Assert.Equal(1, queue.Retried);
        Assert.Equal(1, queue.Poisoned);
        Assert.Equal(3, queue.RecentProcessed);
        Assert.Equal(2, queue.RecentFailed);
        Assert.Equal(200, queue.RecentAverageDuration);
        Assert.Equal(2000, queue.RecentAverageWait);
        Assert.Equal(0, queue.Processing);
    }

    [Fact]
    public void RecentActivityCoversTheLastWindow() {
        using var metrics = new WorkerMetrics(timeProvider: _time);
        _time.Advance(TimeSpan.FromSeconds(30));
        Process(metrics, wait: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(100), MessageOutcome.Succeeded);
        Assert.Equal(1, Assert.Single(metrics.Collect(Window).Queues).RecentProcessed);

        _time.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(1, Assert.Single(metrics.Collect(Window).Queues).RecentProcessed);

        _time.Advance(TimeSpan.FromSeconds(30));
        Process(metrics, wait: TimeSpan.Zero, duration: TimeSpan.FromMilliseconds(500), MessageOutcome.Succeeded);
        var snapshot = metrics.Collect(Window);
        // The first message was processed before the sample of 60 seconds ago.
        Assert.InRange(snapshot.Window, 60, 61);
        var queue = Assert.Single(snapshot.Queues);
        Assert.Equal(1, queue.RecentProcessed);
        Assert.Equal(500, queue.RecentAverageDuration);
        Assert.Equal(2, queue.Succeeded);

        _time.Advance(TimeSpan.FromSeconds(60));
        queue = Assert.Single(metrics.Collect(Window).Queues);
        Assert.Equal(0, queue.RecentProcessed);
        Assert.Null(queue.RecentAverageDuration);
        Assert.Null(queue.RecentAverageWait);
        Assert.Equal(2, queue.Succeeded);
    }

    [Fact]
    public void CollectCountsTheWorkInProgress() {
        using var metrics = new WorkerMetrics(timeProvider: _time);
        metrics.MessageStarted(Queue, _time.GetUtcNow().UtcDateTime);
        metrics.TaskStarted("reports.nightly");

        var snapshot = metrics.Collect(Window);

        var queue = Assert.Single(snapshot.Queues);
        Assert.Equal(1, queue.Processing);
        Assert.Equal(0, queue.RecentProcessed);
        Assert.Equal(0, queue.RecentAverageWait);
        var task = Assert.Single(snapshot.Tasks);
        Assert.Equal(1, task.Running);
        Assert.Null(task.AverageDuration);
        Assert.Null(task.LastDuration);
    }

    [Fact]
    public void CollectReportsTheScheduledTaskRuns() {
        using var metrics = new WorkerMetrics(timeProvider: _time);
        Run(metrics, TimeSpan.FromSeconds(2), failed: false);
        Run(metrics, TimeSpan.FromSeconds(4), failed: true);

        var task = Assert.Single(metrics.Collect(Window).Tasks);

        Assert.Equal("reports.nightly", task.Id);
        Assert.Equal(0, task.Running);
        Assert.Equal(1, task.Succeeded);
        Assert.Equal(1, task.Failed);
        Assert.Equal(3000, task.AverageDuration);
        Assert.Equal(4000, task.LastDuration);
    }

    [Fact]
    public void InstrumentsArePublishedOnTheWorkerMeter() {
        var queue = $"meter-{Guid.NewGuid():N}";
        var measurements = new List<(string Instrument, double Value, Dictionary<string, object?> Tags)>();
        using var listener = new MeterListener {
            InstrumentPublished = (instrument, meterListener) => {
                if (instrument.Meter.Name == WorkerDiagnostics.MeterName) {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        void OnMeasurement(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags) {
            var tagMap = new Dictionary<string, object?>();
            foreach (var tag in tags) {
                tagMap[tag.Key] = tag.Value;
            }
            if (Equals(tagMap.GetValueOrDefault("worker.queue"), queue)) {
                lock (measurements) {
                    measurements.Add((instrument.Name, value, tagMap));
                }
            }
        }
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => OnMeasurement(instrument, value, tags));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => OnMeasurement(instrument, value, tags));
        listener.Start();
        using var metrics = new WorkerMetrics(timeProvider: _time);

        var start = metrics.MessageStarted(queue, _time.GetUtcNow().UtcDateTime.AddSeconds(-3));
        _time.Advance(TimeSpan.FromMilliseconds(250));
        metrics.MessageCompleted(queue, start, MessageOutcome.Poisoned);

        Assert.Equal([1, -1], measurements.Where(x => x.Instrument == "indice.worker.queue.messages.active").Select(x => x.Value));
        Assert.Equal(3, Assert.Single(measurements, x => x.Instrument == "indice.worker.queue.message.wait_time").Value, 3);
        var duration = Assert.Single(measurements, x => x.Instrument == "indice.worker.queue.message.duration");
        Assert.Equal(0.25, duration.Value, 3);
        Assert.Equal("poisoned", duration.Tags["worker.outcome"]);
        var processed = Assert.Single(measurements, x => x.Instrument == "indice.worker.queue.messages.processed");
        Assert.Equal(1, processed.Value);
        Assert.Equal("poisoned", processed.Tags["worker.outcome"]);
    }

    private void Process(WorkerMetrics metrics, TimeSpan wait, TimeSpan duration, MessageOutcome outcome) {
        var start = metrics.MessageStarted(Queue, _time.GetUtcNow().UtcDateTime - wait);
        _time.Advance(duration);
        metrics.MessageCompleted(Queue, start, outcome);
    }

    private void Run(WorkerMetrics metrics, TimeSpan duration, bool failed) {
        var start = metrics.TaskStarted("reports.nightly");
        _time.Advance(duration);
        metrics.TaskCompleted("reports.nightly", start, failed);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;
        public override long GetTimestamp() => _now.UtcTicks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public void Advance(TimeSpan by) => _now += by;
    }
}
