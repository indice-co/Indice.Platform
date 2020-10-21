using System.Threading.Tasks;

namespace Indice.Hosting
{
    /// <summary>
    /// A blueprint for a service that processes a work item.
    /// </summary>
    public abstract class JobHandler
    {
        /// <summary>
        /// Creates a new instance of <see cref="JobHandler"/>.
        /// </summary>
        /// <param name="workItem">The work item to process.</param>
        protected JobHandler(WorkItemBase workItem) => WorkItem = workItem;

        /// <summary>
        /// The work item to process.
        /// </summary>
        public WorkItemBase WorkItem { get; }

        /// <summary>
        /// A function used for processing a scheduled work item.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public abstract Task Process();
    }
}
