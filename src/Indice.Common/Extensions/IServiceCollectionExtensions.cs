using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a fugazi implementation of <see cref="IPushNotificationService"/> that does nothing.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddPushNotificationServiceNoop(this IServiceCollection services) {
            services.TryAddSingleton<IPushNotificationService, PushNotificationServiceNoop>();
            return services;
        }

        /// <summary>
        /// Adds a fugazi implementation of <see cref="ILockManager"/> that does nothing.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddLockManagerNoop(this IServiceCollection services) {
            services.TryAddSingleton<ILockManager, LockManagerNoop>();
            return services;
        }

        /// <summary>
        /// Adds a fugazi implementation of <see cref="ILockManager"/> that does nothing.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddEventDispatcherNoop(this IServiceCollection services) {
            services.TryAddSingleton<IEventDispatcher, EventDispatcherNoop>();
            return services;
        }
    }
}
