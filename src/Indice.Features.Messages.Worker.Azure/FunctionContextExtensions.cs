using Indice.Features.Messages.Core.Manager;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Extension methods on <see cref="FunctionContext"/> class.
    /// </summary>
    public static class FunctionContextExtensions
    {
        /// <summary>
        /// Gets the registered <see cref="NotificationsManager"/> instance from the DI container.
        /// </summary>
        /// <param name="executionContext">Encapsulates the information about a function execution.</param>
        public static NotificationsManager GetNotificationsManager(this FunctionContext executionContext) =>
            (NotificationsManager)executionContext.InstanceServices.GetService(typeof(NotificationsManager));
    }
}
