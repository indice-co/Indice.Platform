using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting.Tasks;

internal class TaskHandlerActivator
{
    private readonly IServiceProvider _serviceProvider;

    public TaskHandlerActivator(IServiceProvider serviceProvider) {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task Invoke(Type jobHandlerType, object state, CancellationToken cancellationToken, object? workItem = null) {
        var methods = jobHandlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        using (var scope = _serviceProvider.CreateScope()) {
            var handler = scope.ServiceProvider.GetService(jobHandlerType);
            var processMethod = methods.Where(x => "Process".Equals(x.Name, StringComparison.OrdinalIgnoreCase)).First();
            var stateType = state?.GetType() ?? typeof(IDictionary<string, object>);
            object?[] arguments;
            if (workItem != null) {
                var workItemType = workItem.GetType();
                arguments = processMethod
                    .GetParameters()
                    .Select(x => x.ParameterType.IsAssignableFrom(workItemType) ? workItem :
                                 x.ParameterType.IsAssignableFrom(typeof(CancellationToken)) ? cancellationToken :
                                 x.ParameterType.IsAssignableFrom(stateType) ? state :
                                 scope.ServiceProvider.GetRequiredService(x.ParameterType))
                    .ToArray();
            } else {
                arguments = processMethod
                    .GetParameters()
                    .Select(x => x.ParameterType.IsAssignableFrom(typeof(CancellationToken)) ? cancellationToken :
                                 x.ParameterType.IsAssignableFrom(stateType) ? state :
                                 scope.ServiceProvider.GetRequiredService(x.ParameterType))
                    .ToArray();
            }
            var isAwaitable = typeof(Task).IsAssignableFrom(processMethod.ReturnType);
            if (isAwaitable) {
                await (Task)processMethod.Invoke(handler, arguments)!;
            } else {
                processMethod.Invoke(handler, arguments);
            }
        }
    }
}
