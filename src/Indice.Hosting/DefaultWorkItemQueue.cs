using System;
using System.Threading.Tasks;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public class DefaultWorkItemQueue : IWorkItemQueue<DefaultWorkItem>
    {
        /// <inheritdoc />
        public void Enqueue(DefaultWorkItem workItem) {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<DefaultWorkItem> Dequeue() {
            throw new NotImplementedException();
        }
    }
}
