using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Hosting.Tasks
{
    public interface IQueueNameResolver<TWorkItem> where TWorkItem : class
    {
        string Resolve();
    }
}
