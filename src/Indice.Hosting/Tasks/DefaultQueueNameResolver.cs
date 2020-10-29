using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Hosting.Tasks
{
    public class DefaultQueueNameResolver<TWorkItem> : IQueueNameResolver<TWorkItem> where TWorkItem : class
    {
        private readonly string _Name;
        public DefaultQueueNameResolver(QueueOptions queueOptions) {
            _Name = queueOptions.QueueName;
        }
        public string Resolve() => _Name ?? typeof(TWorkItem).Name;
    }
}
