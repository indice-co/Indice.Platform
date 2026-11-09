using System.Text.Json;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Workflows;
using Microsoft.Agents.AI.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Agents.Core.Tests;

public class PersistedCheckpointStoreTests : IAsyncLifetime
{

    public ServiceProvider ServiceProvider { get; }

    public PersistedCheckpointStoreTests() {
        var services = new ServiceCollection();
        services.AddDbContext<AgentsDbContext>(builder => builder.UseInMemoryDatabase(databaseName: "AgentsDb"), ServiceLifetime.Singleton);
        ServiceProvider = services.BuildServiceProvider();
    }

    public ValueTask InitializeAsync() {
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync() {
        await ServiceProvider.DisposeAsync();
    }

    [Fact]
    public async Task CreateAndRetrieve_RoundTrips_Payload() {
        var db = ServiceProvider.GetRequiredService<AgentsDbContext>();
        var store = new PersistedCheckpointStore(db);
        var runId = Guid.NewGuid().ToString();
        var payload = JsonDocument.Parse("""{"answer":42,"nested":{"text":"hello"}}""").RootElement;

        var info = await store.CreateCheckpointAsync(runId, payload);
        var retrieved = await store.RetrieveCheckpointAsync(runId, info);

        Assert.Equal(runId, info.SessionId);
        Assert.Equal(42, retrieved.GetProperty("answer").GetInt32());
        Assert.Equal("hello", retrieved.GetProperty("nested").GetProperty("text").GetString());
    }

    [Fact]
    public async Task RetrieveIndex_ReturnsCheckpoints_OldestFirst() {
        var runId = Guid.NewGuid().ToString();
        var first = await CreateCheckpointAsync(runId, """{"step":1}""");
        await Task.Delay(10, TestContext.Current.CancellationToken);
        var second = await CreateCheckpointAsync(runId, """{"step":2}""");

        var db = ServiceProvider.GetRequiredService<AgentsDbContext>();
        var index = (await new PersistedCheckpointStore(db).RetrieveIndexAsync(runId)).ToList();

        Assert.Equal(2, index.Count);
        Assert.Equal(first.CheckpointId, index[0].CheckpointId);
        Assert.Equal(second.CheckpointId, index[1].CheckpointId);
    }

    [Fact]
    public async Task Checkpoints_SurviveAcrossDbContextInstances() {
        var runId = Guid.NewGuid().ToString();
        var info = await CreateCheckpointAsync(runId, """{"conversationState":"persisted"}""");

        // Fresh context simulates a new HTTP request / service scope.
        var db = ServiceProvider.GetRequiredService<AgentsDbContext>();
        var retrieved = await new PersistedCheckpointStore(db).RetrieveCheckpointAsync(runId, info);

        Assert.Equal("persisted", retrieved.GetProperty("conversationState").GetString());
    }

    [Fact]
    public async Task RetrieveCheckpoint_UnknownId_Throws() {
        var db = ServiceProvider.GetRequiredService<AgentsDbContext>();
        var store = new PersistedCheckpointStore(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await store.RetrieveCheckpointAsync("missing-run", new CheckpointInfo("missing-run", "missing-checkpoint")));
    }

    [Fact]
    public async Task CheckpointManager_GetLatestCheckpointAsync_ResolvesMostRecent() {
        var runId = Guid.NewGuid().ToString();
        await CreateCheckpointAsync(runId, """{"step":1}""");
        await Task.Delay(10, TestContext.Current.CancellationToken);
        var latest = await CreateCheckpointAsync(runId, """{"step":2}""");

        var db = ServiceProvider.GetRequiredService<AgentsDbContext>();
        var manager = CheckpointManager.CreateJson(new PersistedCheckpointStore(db));
        var resolved = await manager.GetLatestCheckpointAsync(runId, TestContext.Current.CancellationToken);

        Assert.NotNull(resolved);
        Assert.Equal(latest.CheckpointId, resolved!.CheckpointId);
    }

    private async Task<CheckpointInfo> CreateCheckpointAsync(string runId, string json) {
        var db = ServiceProvider.GetRequiredService<AgentsDbContext>();
        return await new PersistedCheckpointStore(db).CreateCheckpointAsync(runId, JsonDocument.Parse(json).RootElement);
    }
}
