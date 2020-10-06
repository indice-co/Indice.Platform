using System;
using Indice.Hosting;
using Indice.Services;
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
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configureAction"></param>
        /// <returns>The builder used to configure the worker host.</returns>
        public static WorkerHostBuilder AddWorkerHost(this IServiceCollection services, Action<WorkerHostOptions> configureAction) {
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<IJobFactory, JobFactory>();
            services.AddSingleton<QuartzJobRunner>();
            services.AddHostedService<QueuedHostedService>();
            configureAction.Invoke(new WorkerHostOptions(services));
            return new WorkerHostBuilder(services);
        }

        /// <summary>
        /// Uses Azure Storage service as the store for distributed locking.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> that configures the worker host.</param>
        /// <returns>The <see cref="StorageBuilder"/> used to configure locking and queue persistence.</returns>
        public static StorageBuilder UseAzureStorageLock(this WorkerHostOptions options) => options.UseLock<LockManagerAzure>();

        /// <summary>
        /// Manages access to queues using an in-memory mechanism. Not suitable for distributed scenarios.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> that configures the worker host.</param>
        /// <returns>The <see cref="StorageBuilder"/> used to configure locking and queue persistence.</returns>
        public static StorageBuilder UseInMemoryLock(this WorkerHostOptions options) => options.UseLock<LockManagerInMemory>();

        /// <summary>
        /// Registers an implementation of <see cref="ILockManager"/> which is used for distributed locking.
        /// </summary>
        /// <typeparam name="TLockManager">The concrete type of <see cref="ILockManager"/> to use.</typeparam>
        /// <param name="options">The <see cref="StorageBuilder"/> used to configure locking and queue persistence.</param>
        public static StorageBuilder UseLock<TLockManager>(this WorkerHostOptions options) where TLockManager : ILockManager {
            options.Services.AddSingleton(typeof(ILockManager), typeof(TLockManager));
            return new StorageBuilder(options.Services);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static StorageBuilder UseInMemoryStorage(this WorkerHostOptions options) {
            //options.Services.AddSingleton(typeof(IWorkItemQueue<>).MakeGenericType(typeof(TWorkItem)), typeof(WorkItemQueueInMemory<>).MakeGenericType(typeof(TWorkItem)));
            return new StorageBuilder(options.Services);
        }

        public static StorageBuilder UseStorage<TWorkItemQueueType>(this WorkerHostOptions options) {
            //options.Services.AddSingleton(typeof(IWorkItemQueue<>).MakeGenericType(typeof(TWorkItem)), typeof(WorkItemQueueInMemory<>).MakeGenericType(typeof(TWorkItem)));
            return new StorageBuilder(options.Services);
        }

        /// <summary>
        /// Registers a job that will be processed by the worker host. Usually followed by a <see cref="WithQueueTrigger{TWorkItem}(JobTriggerBuilder, Action{QueueOptions})"/> call to configure the way that a job is triggered.
        /// </summary>
        /// <typeparam name="TJobHandler">The type of the class that will handle the job. Must inherit from <see cref="JobHandler"/>.</typeparam>
        /// <param name="builder">A helper class to configure the worker host.</param>
        /// <returns></returns>
        public static JobTriggerBuilder AddJob<TJobHandler>(this WorkerHostBuilder builder) where TJobHandler : JobHandler {
            builder.Services.AddScoped<TJobHandler>();
            return new JobTriggerBuilder(builder.Services, typeof(TJobHandler));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureAction"></param>
        /// <returns></returns>
        public static WorkerHostBuilder WithQueueTrigger<TWorkItem>(this JobTriggerBuilder builder, Action<QueueOptions> configureAction = null) where TWorkItem : WorkItem {
            var options = new QueueOptions {
                Services = builder.Services
            };
            configureAction?.Invoke(options);
            options.Services.AddSingleton(typeof(DequeueJob<>).MakeGenericType(typeof(TWorkItem)));
            options.Services.AddSingleton(serviceProvider => new DequeueJobSchedule(builder.JobHandlerType, typeof(TWorkItem), options.QueueName, options.PollingIntervalInSeconds));
            return new WorkerHostBuilder(options.Services);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TWorkItem"></typeparam>
        /// <param name="builder"></param>
        /// <param name="queueName"></param>
        /// <param name="pollingIntervalInSeconds"></param>
        /// <returns></returns>
        public static WorkerHostBuilder WithQueueTrigger<TWorkItem>(this JobTriggerBuilder builder, string queueName, int pollingIntervalInSeconds) where TWorkItem : WorkItem =>
            builder.WithQueueTrigger<TWorkItem>(options => new QueueOptions {
                Services = builder.Services,
                QueueName = queueName,
                PollingIntervalInSeconds = pollingIntervalInSeconds
            });
    }
}
