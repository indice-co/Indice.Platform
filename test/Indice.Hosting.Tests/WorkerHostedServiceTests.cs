using System.Collections.Specialized;
using Indice.Hosting.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Indice.Hosting.Tests;

public class WorkerHostedServiceTests
{
    [Fact]
    public async Task HeartbeatRunsWithFullQuartzCapacityAndWhileJobsDrain() {
        var cancellationToken = TestContext.Current.CancellationToken;
        var schedulerFactory = new StdSchedulerFactory(new NameValueCollection {
            ["quartz.scheduler.instanceName"] = $"heartbeat-test-{Guid.NewGuid():N}",
            ["quartz.threadPool.maxConcurrency"] = "1"
        });
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var job = new Mock<IJob>();
        job.Setup(x => x.Execute(It.IsAny<IJobExecutionContext>())).Returns(async () => {
            started.TrySetResult();
            await release.Task;
        });
        var jobFactory = new Mock<IJobFactory>();
        jobFactory.Setup(x => x.NewJob(It.IsAny<TriggerFiredBundle>(), It.IsAny<IScheduler>())).Returns(job.Object);
        await scheduler.ScheduleJob(
            JobBuilder.Create<BlockingJob>().Build(),
            TriggerBuilder.Create().StartNow().Build(),
            cancellationToken
        );
        using var heartbeatAttempts = new SemaphoreSlim(0);
        var attemptCount = 0;
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(x => x.ServiceProvider).Returns(Mock.Of<IServiceProvider>());
        var scopeFactory = new Mock<IServiceScopeFactory>();
        // A missing database is retried at every heartbeat; no SQL Server is needed to observe the loop.
        scopeFactory.Setup(x => x.CreateScope()).Returns(() => {
            Interlocked.Increment(ref attemptCount);
            heartbeatAttempts.Release();
            return scope.Object;
        });
        var registry = new WorkerServerRegistry(
            scopeFactory.Object,
            new ServerRegistrationSettings(TimeSpan.FromMilliseconds(20), TimeSpan.FromMinutes(5)),
            [], [], NullLogger<WorkerServerRegistry>.Instance
        );
        using var service = new WorkerHostedService(
            schedulerFactory, NullLogger<WorkerHostedService>.Instance, [], [], jobFactory.Object,
            Options.Create(new WorkerLifetimeOptions { WaitJobsToCompleteOnShutdown = true }), [], [registry]
        );
        using var startupCancellation = new CancellationTokenSource();
        try {
            await service.StartAsync(startupCancellation.Token);
            startupCancellation.Cancel();
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            Assert.Single(await scheduler.GetCurrentlyExecutingJobs(cancellationToken));
            // Consume startup registration, then prove the heartbeat runs with the only Quartz slot occupied.
            Assert.True(await heartbeatAttempts.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken));
            Assert.True(await heartbeatAttempts.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken));

            var stopping = service.StopAsync(cancellationToken);
            var attemptsBeforeDrain = Volatile.Read(ref attemptCount);
            while (Volatile.Read(ref attemptCount) <= attemptsBeforeDrain) {
                Assert.True(await heartbeatAttempts.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken));
            }
            Assert.False(stopping.IsCompleted);

            release.TrySetResult();
            await stopping.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            var attemptsAfterStop = Volatile.Read(ref attemptCount);
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            Assert.Equal(attemptsAfterStop, Volatile.Read(ref attemptCount));
        } finally {
            release.TrySetResult();
            await service.StopAsync(CancellationToken.None);
        }
    }

    public sealed class BlockingJob : IJob
    {
        public Task Execute(IJobExecutionContext context) => Task.CompletedTask;
    }
}
