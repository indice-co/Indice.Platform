using System.Threading.Tasks;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TWorkItem"></typeparam>
    public abstract class WorkItemHandler<TWorkItem> where TWorkItem : WorkItem
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="workItem"></param>
        /// <returns></returns>
        public abstract Task Process(TWorkItem workItem);
    }
}
