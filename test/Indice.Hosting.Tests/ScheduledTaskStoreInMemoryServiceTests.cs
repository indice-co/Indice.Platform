using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Hosting.Tests;

public class ScheduledTaskStoreInMemoryServiceTests
{
    private const int TestTaskExecutionCount = 10;
    private const string TestTaskCronExpression = "* * * ? * * *";
    private const string TestTask2CronExpression = "* * * ? * * *";
    private readonly IWebHost _host;
    private static int _counter = -1; // We have to initialize this at -1 since the first run is immediate.
    private static int _countdownCounter = TestTaskExecutionCount + 1; // We have to initialize this at TEST_TASK_EXECUTION_COUNT + 1 since the first run is immediate.

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
        _host = new TestServer(builder).Host;
    }

    [Fact]
    public async Task ScheduledTaskStoreInMemory_Runs_ReadyForExecutionTasks() {
        await Task.Delay(TestTaskExecutionCount * 1000);
        await _host.StopAsync();
        Assert.Equal(TestTaskExecutionCount, _counter);
        Assert.Equal(0, _countdownCounter);
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
