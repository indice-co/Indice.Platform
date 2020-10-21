using System;
using System.Linq;
using System.Reflection;

namespace Indice.Hosting
{
    internal class JobHandlerFactory : IJobHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public JobHandlerFactory(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public JobHandler Create(Type jobHandlerType, WorkItemBase workItem) {
            var constructors = jobHandlerType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            var constructor = constructors.OrderByDescending(x => x.GetParameters().Length).First();
            var arguments = constructor
                .GetParameters()
                .Select(x => typeof(WorkItemBase).IsAssignableFrom(x.ParameterType) ? workItem : _serviceProvider.GetService(x.ParameterType))
                .ToArray();
            dynamic jobHandler = Convert.ChangeType(constructor.Invoke(arguments), jobHandlerType);
            return jobHandler;
        }
    }
}
