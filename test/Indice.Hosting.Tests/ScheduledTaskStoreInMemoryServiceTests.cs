using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting.Tests;

public class ScheduledTaskStoreInMemoryServiceTests
{
    private const int TestTaskExecutionCount = 3;
    private const string TestTaskCronExpression = "* * * ? * * *";
    private const string TestTask2CronExpression = "* * * ? * * *";
    private readonly WebHostBuilder _builder;
    private static int _counter = 0; // We have to initialize this at -1 since the first run is immediate.
    private static int _countdownCounter = TestTaskExecutionCount; // We have to initialize this at TEST_TASK_EXECUTION_COUNT + 1 since the first run is immediate.

    public ScheduledTaskStoreInMemoryServiceTests() {
        var builder = new WebHostBuilder();
        builder.ConfigureServices(services => {
            services.AddWorkerHost(options => {
                options.WaitJobsToCompleteOnShutdown = true;
                options.UseScheduledTaskStoreInMemory();
                })
            .AddJob<TestTask>()
            .WithScheduleTrigger(TestTaskCronExpression, options => {
                options.Singleton = true;
                options.Name = nameof(TestTask);
                options.Group = nameof(TestTask);
                options.Description = "Asserts true. Is a singleton";
            })
            .AddJob<TestTask2>()
            .WithScheduleTrigger(TestTask2CronExpression, options => {
                options.Name = nameof(TestTask2);
                options.Group = nameof(TestTask2);
                options.Description = "Asserts true. Is not a singleton";
            });

            services.AddRouting();
        });
        builder.Configure(app => {
            app.UseRouting();
            app.UseEndpoints(e => e.MapGet("/health", async context => {
                await Task.CompletedTask;
            }));
        });
        _builder = builder;
    }

    [Fact]
    public async Task ScheduledTaskStoreInMemory_Runs_ReadyForExecutionTasks() {
        using var testServer = new TestServer(_builder);
        var host = testServer.Host;
        var delayTimeInMiliseconds = Math.Max(0, TestTaskExecutionCount - 1) * 1000;
        await Task.Delay(delayTimeInMiliseconds, TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);
        Assert.True(_counter >= TestTaskExecutionCount, userMessage: $"Test run Count expected to be at least '{TestTaskExecutionCount}' but was actually '{_counter}' -  at least {TestTaskExecutionCount} times execution");
        Assert.True(_countdownCounter <= 0, userMessage: $"Countdown was expected to be less than or equal to zero but was actually '{_countdownCounter}' - at least {TestTaskExecutionCount} times execution");
    }

    private sealed class TestTask
    {
        public Task Process() {
            Console.WriteLine($"Countup: {_counter++}");
            return Task.CompletedTask;
        }
    }

    private sealed class TestTask2
    {
        public Task Process() {
            Console.WriteLine($"Countdown: {_countdownCounter--}");
            return Task.CompletedTask;
        }
    }
}
