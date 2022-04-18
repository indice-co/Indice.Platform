using System.Threading.Tasks;
using Indice.Hosting.Models;

namespace Indice.Hosting.Services
{
    /// <summary>
    /// Scheduled task store abstraction.
    /// </summary>
    public interface IScheduledTaskStore<TState> where TState : class
    {
        /// <summary>
        /// Persist the schedule task.
        /// </summary>
        /// <param name="scheduledTask">The task item to persist.</param>
        Task Save(ScheduledTask<TState> scheduledTask);
        /// <summary>
        /// Find a persisted schedule task.
        /// </summary>
        /// <param name="taskId">The unique id of the task item.</param>
        Task<ScheduledTask<TState>> GetById(string taskId);
    }
}
