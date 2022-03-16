using System.Threading.Tasks;
using Indice.Hosting.Models;

namespace Indice.Hosting.Services
{
    /// <summary>
    /// Not operational implementation for <see cref="IScheduledTaskStore{TState}"/>.
    /// </summary>
    /// <typeparam name="TState">The type of state object.</typeparam>
    public class NoOpScheduledTaskStore<TState> : IScheduledTaskStore<TState> where TState : class
    {
        /// <inheritdoc/>
        public Task<ScheduledTask<TState>> GetById(string taskId) => Task.FromResult(new ScheduledTask<TState>());

        /// <inheritdoc/>
        public Task Save(ScheduledTask<TState> scheduledTask) => Task.CompletedTask;
    }
}
