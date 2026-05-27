using System.Text;
using System.Text.Json;
using Indice.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Indice.Services.Tests;

public class LockManagerAzureTests
{
    private readonly ILockManager _LockManager;
    private readonly IFileService _FileService;

    public LockManagerAzureTests() {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:Storage"] =
                    "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<AzureClientFactory>();

        services.AddSingleton(new LockManagerAzureOptions {
            EnvironmentName = "test",
            ConnectionStringName = "Storage"
        });

        services.AddSingleton<ILockManager, LockManagerAzure>();

        services.AddTransient<IFileService>(sp =>
            new FileServiceAzureStorage(            
                sp.GetRequiredService<AzureClientFactory>(),
                "test",
                null));

        var provider = services.BuildServiceProvider();

        _LockManager = provider.GetRequiredService<ILockManager>();
        _FileService = provider.GetRequiredService<IFileService>();
    }

    [Fact(Skip = "Should integrate azurite on build yaml")]
    public async Task AcquireLockTest() {
        var duration = TimeSpan.FromSeconds(15);
        var name = "constantinos"; // using a random name :)
        var @lock = await _LockManager.AcquireLock(name, duration);
        await using (@lock) {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
        }
        var @lock2 = await _LockManager.AcquireLock(name, duration);
        await using (@lock2) {
            await Task.Delay(TimeSpan.FromSeconds(0.5));
        }
        var result = await _LockManager.TryAcquireLock(name);
        if (result.Ok) {
            await using (result.Lock) {
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }
        }
    }

    [Fact(Skip = "Should integrate azurite on build yaml")]
    public async Task AcquireLock_Should_ThrowException_OnInvalidDurationValues() {
        var durationLessThanMin = TimeSpan.FromSeconds(10);
        var durationGreaterThanMax = TimeSpan.FromSeconds(100);
        var name = "constantinos"; // using a random name :)

        await Assert.ThrowsAsync<LockManagerException>(() => _LockManager.AcquireLock(name, durationLessThanMin));
        await Assert.ThrowsAsync<LockManagerException>(() => _LockManager.AcquireLock(name, durationGreaterThanMax));
    }

    [Fact(Skip = "Only for debug purposes")]
    public async Task FunctionLockingTestMaster() {
        var duration = TimeSpan.FromSeconds(60);
        var operation = "MasterProductImport"; // using a random name :)
        var @lock = await _LockManager.AcquireLock(operation, duration);
        await _FileService.SaveAsync($"messages/{operation}.json", Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Tuple<string, string>(@lock.LeaseId, @lock.Name))));
    }

    [Fact(Skip = "Only for debug purposes")]
    public async Task FunctionLockingTestDetail() {
        var operation = "MasterProductImport"; // using a random name :)
        var bytes = await _FileService.GetAsync($"messages/{operation}.json");
        var message = JsonSerializer.Deserialize<(string LeaseId, string Name)>(Encoding.UTF8.GetString(bytes), JsonSerializerOptionDefaults.GetDefaultSettings());
        var @lock = await _LockManager.Renew(message.Name, message.LeaseId);
        await Task.Delay(TimeSpan.FromSeconds(10));
    }

    [Fact(Skip = "Only for debug purposes")]
    public async Task FunctionLockingExclusiveRunTest() {
        var operation = "MasterProductImportExclusive"; // using a random name :)
        await _LockManager.ExclusiveRun(operation, async (token) => {
            await Task.Delay(TimeSpan.FromSeconds(10), token);
            Console.WriteLine("operation run...");
        }, cancellationToken: default, new ExclusiveRunOptions {
            LockDuration = 30,
            RetryIntervalInSeconds = null
        });
    }

    [Fact(Skip = "Only for debug purposes")]
    public async Task FunctionLockingExclusiveRun_WillNotEnter_NeverEndingLoop_Test() {
        var operation = "MasterProductImportExclusive"; // using a random name :)

        var source = new CancellationTokenSource();
        source.CancelAfter(TimeSpan.FromSeconds(2));

        using var lock1 = await _LockManager.AcquireLock(operation, TimeSpan.FromSeconds(59));

        var exclusiveRunTask = _LockManager.ExclusiveRun(operation, async (token) => {
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            Console.WriteLine("operation run...");
        }, cancellationToken: source.Token, new ExclusiveRunOptions {
            LockDuration = 30,
            RetryIntervalInSeconds = 1
        });

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await exclusiveRunTask);
    }
}
