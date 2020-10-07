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
        /// Registers a hosted service that manages and configures the lifetime of background tasks.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configureAction">The delegate used to configure the worker host options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilder AddWorkerHost(this IServiceCollection services, Action<WorkerHostOptions> configureAction) {
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<IJobFactory, JobFactory>();
            services.AddSingleton<IJobHandlerFactory, JobHandlerFactory>();
            services.AddSingleton<QuartzJobRunner>();
            services.AddHostedService<QueuedHostedService>();
            var workerHostOptions = new WorkerHostOptions(services);
            configureAction.Invoke(workerHostOptions);
            return new WorkerHostBuilder(services, workerHostOptions.WorkItemQueueType);
        }

        /// <summary>
        /// Uses Azure Storage service as the store for distributed locking.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseAzureStorageLock(this WorkerHostOptions options) => options.UseLock<LockManagerAzure>();

        /// <summary>
        /// Manages access to queues using an in-memory mechanism. Not suitable for distributed scenarios.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseInMemoryLock(this WorkerHostOptions options) => options.UseLock<LockManagerInMemory>();

        /// <summary>
        /// Registers an implementation of <see cref="ILockManager"/> which is used for distributed locking.
        /// </summary>
        /// <typeparam name="TLockManager">The concrete type of <see cref="ILockManager"/> to use.</typeparam>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseLock<TLockManager>(this WorkerHostOptions options) where TLockManager : ILockManager {
            options.Services.TryAddSingleton(typeof(ILockManager), typeof(TLockManager));
            return options;
        }

        /// <summary>
        /// Uses an in-memory storage mechanism in order to manage queue items.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseInMemoryStorage(this WorkerHostOptions options) => options.UseStorage(typeof(WorkItemQueueInMemory<>));

        /// <summary>
        /// Registers a custom work item queue.
        /// </summary>
        /// <typeparam name="TWorkItemQueue">The type of the work item queue.</typeparam>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseStorage<TWorkItemQueue>(this WorkerHostOptions options) where TWorkItemQueue : IWorkItemQueue<WorkItem> =>
            options.UseStorage(typeof(TWorkItemQueue));

        /// <summary>
        /// Registers a job that will be processed by the worker host. Usually followed by a <see cref="WithQueueTrigger{TWorkItem}(JobTriggerBuilder, Action{QueueOptions})"/> call to configure the way that a job is triggered.
        /// </summary>
        /// <typeparam name="TJobHandler">The type of the class that will handle the job. Must inherit from <see cref="JobHandler"/>.</typeparam>
        /// <param name="builder">The <see cref="WorkerHostBuilder"/> used to configure the worker host.</param>
        /// <returns>The <see cref="JobTriggerBuilder"/> used to configure the way that a job is triggered.</returns>
        public static JobTriggerBuilder AddJob<TJobHandler>(this WorkerHostBuilder builder) where TJobHandler : JobHandler =>
            new JobTriggerBuilder(builder.Services, typeof(TJobHandler), builder.WorkItemQueueType);

        /// <summary>
        /// Specifies that the configured job will be triggered by an item inserted to the a queue.
        /// </summary>
        /// <param name="builder">The <see cref="JobTriggerBuilder"/> used to configure the way that a job is triggered.</param>
        /// <param name="configureAction">The delegate used to configure the queue options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilder WithQueueTrigger<TWorkItem>(this JobTriggerBuilder builder, Action<QueueOptions> configureAction = null) where TWorkItem : WorkItem {
            var options = new QueueOptions(builder.Services);
            configureAction?.Invoke(options);
            options.Services.AddSingleton(typeof(IWorkItemQueue<>).MakeGenericType(typeof(TWorkItem)), builder.WorkItemQueueType.MakeGenericType(typeof(TWorkItem)));
            options.Services.AddSingleton(typeof(DequeueJob<>).MakeGenericType(typeof(TWorkItem)));
            options.Services.AddSingleton(serviceProvider => new DequeueJobSchedule(builder.JobHandlerType, typeof(TWorkItem), options.QueueName, options.PollingIntervalInSeconds));
            return new WorkerHostBuilder(options.Services, builder.WorkItemQueueType);
        }

        /// <summary>
        /// Specifies that the configured job will be triggered by an item inserted to the a queue.
        /// </summary>
        /// <typeparam name="TWorkItem"></typeparam>
        /// <param name="builder">The <see cref="JobTriggerBuilder"/> used to configure the way that a job is triggered.</param>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="pollingIntervalInSeconds">Specifies the time interval between two attempts to dequeue new items.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilder WithQueueTrigger<TWorkItem>(this JobTriggerBuilder builder, string queueName, int pollingIntervalInSeconds) where TWorkItem : WorkItem =>
            builder.WithQueueTrigger<TWorkItem>(options => new QueueOptions(builder.Services) {
                QueueName = queueName,
                PollingIntervalInSeconds = pollingIntervalInSeconds
            });

        private static WorkerHostOptions UseStorage(this WorkerHostOptions options, Type workItemQueueType) {
            options.WorkItemQueueType = workItemQueueType;
            return options;
        }
    }
}
