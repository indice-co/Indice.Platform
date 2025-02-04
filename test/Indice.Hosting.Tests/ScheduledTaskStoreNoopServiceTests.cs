using Indice.Hosting.Models;
using Indice.Hosting.Services;
using Xunit;

namespace Indice.Hosting.Tests;

public class ScheduledTaskStoreNoopServiceTests
{
    private readonly ScheduledTaskStoreNoop<Dictionary<string, object>> _scheduledTaskStoreNoop = new();

    public ScheduledTaskStoreNoopServiceTests() {
    }

    [Fact]
    public async Task ScheduledTaskStoreNoop_Returns_ReadyForExecutionTask() {
        var task = await _scheduledTaskStoreNoop.GetById(Guid.NewGuid().ToString("N"));
        Assert.NotNull(task);
        Assert.True(task.Enabled);
    }
}
