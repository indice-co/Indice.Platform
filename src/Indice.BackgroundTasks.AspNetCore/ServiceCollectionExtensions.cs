using Indice.BackgroundTasks;
using Indice.BackgroundTasks.Abstractions;
using Indice.BackgroundTasks.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods on <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds functionality for processing tasks outside of the request lifecycle of the ASP.NET Core app. Tasks are processed as a FIFO thread-safe queue.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddBackgroundTasks(this IServiceCollection services) {
            services.AddHostedService<BackgroundTaskHostedService>();
            services.AddSingleton<BackgroundTaskServer>();
            services.AddDefaultBackgroundTaskQueue();
            return services;
        }

        private static IServiceCollection AddDefaultBackgroundTaskQueue(this IServiceCollection services) {
            services.AddSingleton<IBackgroundTaskQueue, InMemoryBackgroundTaskQueue>();
            return services;
        }
    }
}
