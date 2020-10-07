using System;

namespace Indice.Hosting
{
    internal interface IJobHandlerFactory
    {
        JobHandler Create(Type jobHandlerType, WorkItem workItem);
    }
}
