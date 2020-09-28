using System.Threading.Tasks;

namespace Indice.Hosting
{
    /// <summary>
    /// A blueprint for a service that handles a work item.
    /// </summary>
    /// <typeparam name="TWorkItem">The CLR type of the work item.</typeparam>
    public interface IWorkItemHandler<TWorkItem> where TWorkItem : WorkItem
    {
        /// <summary>
        /// A function used for processing the work item.
        /// </summary>
        /// <param name="workItem">The work item to process.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task Process(TWorkItem workItem);
    }
}
