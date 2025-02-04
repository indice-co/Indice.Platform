using System.Collections.Concurrent;
using Indice.Hosting.Models;

namespace Indice.Hosting.Services;

/// <summary>In memory implementation of <see cref="IScheduledTaskStore{TState}"/>.</summary>
/// <typeparam name="TState">The type of state object.</typeparam>
public class ScheduledTaskStoreInMemory<TState> : IScheduledTaskStore<TState> where TState : class
{
    private readonly ConcurrentDictionary<string, ScheduledTask<TState>> _tasks = new();

    /// <inheritdoc/>
    public Task<ScheduledTask<TState>?> GetById(string taskId)
    {
        _tasks.TryGetValue(taskId, out var task);
        return Task.FromResult(task);
    }

    /// <inheritdoc/>
    public Task Save(ScheduledTask<TState> scheduledTask)
    {
        _tasks.AddOrUpdate(scheduledTask.Id, scheduledTask, (_, _) => scheduledTask);
        return Task.CompletedTask;
    }
}
