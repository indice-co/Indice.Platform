using Indice.Features.Messages.Core.Manager;

namespace Microsoft.Azure.Functions.Worker;

/// <summary>Extension methods on <see cref="FunctionContext"/> class.</summary>
public static class FunctionContextExtensions
{
    /// <summary>Gets the registered <see cref="NotificationsManager"/> instance from the DI container.</summary>
    /// <param name="functionContext">Encapsulates the information about a function execution.</param>
    public static NotificationsManager GetNotificationsManager(this FunctionContext functionContext) =>
        (NotificationsManager)functionContext.InstanceServices.GetService(typeof(NotificationsManager));
}
