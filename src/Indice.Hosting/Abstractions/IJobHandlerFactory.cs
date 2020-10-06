using System;

namespace Indice.Hosting.Abstractions
{
    /// <summary>
    /// 
    /// </summary>
    internal interface IJobHandlerFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobHandlerType"></param>
        /// <param name="workItem"></param>
        /// <returns></returns>
        JobHandler Create(Type jobHandlerType, WorkItem workItem);
    }
}
