using System;
using Indice.Hosting.Abstractions;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    internal class JobHandlerFactory : IJobHandlerFactory
    {
        /// <inheritdoc />
        public JobHandler Create(Type jobHandlerType, WorkItem workItem) {
            throw new NotImplementedException();
        }
    }
}
