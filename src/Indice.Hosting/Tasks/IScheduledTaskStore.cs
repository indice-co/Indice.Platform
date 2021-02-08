using System.Threading.Tasks;

namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// Scheduled task store abstraction.
    /// </summary>
    public interface IScheduledTaskStore<TState> where TState : class
    {
        /// <summary>
        /// Persist the schedule task.
        /// </summary>
        /// <param name="scheduledTask"></param>
        /// <returns></returns>
        Task Save(ScheduledTask<TState> scheduledTask);

        /// <summary>
        /// Find a persisted schedule task
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        Task<ScheduledTask<TState>> GetById(string taskId);
    }
}
