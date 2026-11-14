using Indice.Features.Workers.Server.Models;
using Indice.Features.Workers.Server.Services;
using Indice.Hosting.Data.Models;

namespace Indice.Hosting.Tests;

public class WorkerDashboardMetricsTests
{
    [Fact]
    public void QueueWaitsAreWeightedByStartsIncludingWorkInProgress() {
        var activity = WorkerDashboardService.QueueActivity.From([
            (new DbServerQueueMetrics { RecentStarted = 3, RecentProcessed = 0, RecentAverageWait = 1000 }, 60d),
            (new DbServerQueueMetrics { RecentStarted = 1, RecentProcessed = 10, RecentAverageWait = 5000 }, 60d),
            (new DbServerQueueMetrics { RecentStarted = 0, RecentProcessed = 20, RecentAverageWait = 9000 }, 60d)
        ]);

        Assert.Equal(2000, activity.AverageWait);
        Assert.Equal(30, activity.ProcessedPerMinute);
    }

    [Fact]
    public void QueueWaitsRetainLegacyWeightingWhenStartCountsAreMissing() {
        var activity = WorkerDashboardService.QueueActivity.From([
            (new DbServerQueueMetrics { RecentProcessed = 3, RecentAverageWait = 1000 }, 60d),
            (new DbServerQueueMetrics { RecentProcessed = 0, RecentAverageWait = 5000 }, 60d)
        ]);

        Assert.Equal(2000, activity.AverageWait);
    }

    [Fact]
    public void LastTaskDurationComesFromTheLatestCompletionNotTheNewestProcess() {
        var now = DateTime.UtcNow;
        var details = new ScheduledTaskDetails { Id = "reports.nightly", WorkerId = "worker" };

        WorkerDashboardService.ApplyTaskMetrics(details, [
            Server("worker", now.AddHours(-2), now.AddMinutes(-1), 4000),
            Server("worker", now.AddHours(-1), now.AddMinutes(-5), 2000),
            Server("other-worker", now.AddHours(-3), now, 6000),
            Server("worker", now, now.AddMinutes(1), 8000, isAlive: false)
        ]);

        Assert.Equal(6000, details.LastDuration);
        Assert.Equal(4000, details.AverageDuration);
    }

    [Fact]
    public void LastTaskDurationRetainsTheLegacyMachineAndStartupFallback() {
        var now = DateTime.UtcNow;
        var details = new ScheduledTaskDetails { Id = "reports.nightly", WorkerId = "worker" };

        WorkerDashboardService.ApplyTaskMetrics(details, [
            Server("worker", now.AddHours(-2), null, 4000),
            Server("worker", now.AddHours(-1), null, 2000),
            Server("other-worker", now, null, 6000)
        ]);

        Assert.Equal(2000, details.LastDuration);
    }

    [Fact]
    public void MixedVersionTaskDurationsFallBackWhenCompletionOrderIsUnknown() {
        var now = DateTime.UtcNow;
        var details = new ScheduledTaskDetails { Id = "reports.nightly", WorkerId = "legacy-worker" };

        WorkerDashboardService.ApplyTaskMetrics(details, [
            Server("worker", now.AddHours(-2), now.AddMinutes(-5), 4000),
            Server("legacy-worker", now.AddHours(-1), null, 2000)
        ]);

        Assert.Equal(2000, details.LastDuration);
    }

    private static WorkerDashboardService.RegisteredServer Server(string machine, DateTime startedAt, DateTime? completedAt, double duration, bool isAlive = true) =>
        new(new DbServer {
            MachineName = machine,
            StartedAt = startedAt,
            Metrics = DbServer.Serialize(new DbServerMetrics {
                Tasks = [new DbServerTaskMetrics {
                    Id = "reports.nightly",
                    Succeeded = 1,
                    AverageDuration = duration,
                    LastDuration = duration,
                    LastCompletedAt = completedAt
                }]
            })
        }, isAlive);
}
