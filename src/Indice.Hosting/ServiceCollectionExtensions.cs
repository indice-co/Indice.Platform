using System;
using Indice.Hosting;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods on <see cref="IServiceCollection"/> that help register required services for background task processing.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureAction"></param>
        /// <returns>The builder used to configure the background tasks.</returns>
        public static BackgroundTasksBuilder AddBackgroundTasks(this IServiceCollection services, Action<BackgroundTasksBuilder> configureAction) {
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<IJobFactory, JobFactory>();
            services.AddSingleton<QuartzJobRunner>();
            services.AddHostedService<QueuedHostedService>();
            var builder = new BackgroundTasksBuilder(services);
            configureAction.Invoke(builder);
            services.TryAddSingleton<ILockManager, DefaultLockManager>();
            return builder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TWorkItemHandler"></typeparam>
        /// <typeparam name="TWorkItem"></typeparam>
        /// <param name="builder"></param>
        /// <param name="configureAction"></param>
        /// <returns></returns>
        public static QueueBuilder AddQueue<TWorkItemHandler, TWorkItem>(this BackgroundTasksBuilder builder, Action<QueueOptions> configureAction = null)
            where TWorkItemHandler : IWorkItemHandler<TWorkItem>
            where TWorkItem : WorkItem {
            var options = new QueueOptions {
                Services = builder.Services
            };
            configureAction?.Invoke(options);
            options.Services.AddSingleton(typeof(DequeueJob<>).MakeGenericType(typeof(TWorkItem)));
            options.Services.AddSingleton(typeof(IWorkItemQueue<>).MakeGenericType(typeof(TWorkItem)), typeof(DefaultWorkItemQueue<>).MakeGenericType(typeof(TWorkItem)));
            options.Services.AddSingleton(typeof(IWorkItemHandler<>).MakeGenericType(typeof(TWorkItem)), typeof(TWorkItemHandler));
            options.Services.AddSingleton(serviceProvider => new DequeueJobSchedule(typeof(TWorkItem), options.QueueName, options.PollingIntervalInSeconds));
            return new QueueBuilder(options.Services);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TLockManager">The concrete type of <see cref="ILockManager"/> to use.</typeparam>
        /// <param name="options"></param>
        public static void UseLockManager<TLockManager>(this QueueOptions options) where TLockManager : ILockManager {
            options.Services.RemoveAll<ILockManager>();
            options.Services.AddSingleton(typeof(ILockManager), typeof(TLockManager));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TWorkItemQueue"></typeparam>
        /// <typeparam name="TWorkItem"></typeparam>
        /// <param name="options"></param>
        public static void UseWorkItemQueue<TWorkItemQueue, TWorkItem>(this QueueOptions options) where TWorkItemQueue : IWorkItemQueue<TWorkItem> where TWorkItem : WorkItem {

        }
    }

    /// <summary>
    /// A helper class to configure the background tasks.
    /// </summary>
    public class BackgroundTasksBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="BackgroundTasksBuilder"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public BackgroundTasksBuilder(IServiceCollection services) => Services = services;

        /// <summary>
        /// Specifies the contract for a collection of service descriptors.
        /// </summary>
        public IServiceCollection Services { get; }
    }

    /// <summary>
    /// A helper class to configure a queue.
    /// </summary>
    public class QueueBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="QueueBuilder"/>.
        /// </summary>
        /// <param name="services"></param>
        public QueueBuilder(IServiceCollection services) => Services = services;

        /// <summary>
        /// Specifies the contract for a collection of service descriptors.
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
