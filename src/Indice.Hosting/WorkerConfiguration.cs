using System;
using System.Collections.Generic;
using Indice.Hosting;
using Indice.Hosting.Tasks;
using Indice.Hosting.Tasks.Data;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods on <see cref="IServiceCollection"/> that help register required services for background task processing.
    /// </summary>
    public static class WorkerConfigurationExtensions
    {
        /// <summary>
        /// Registers a hosted service that manages and configures the lifetime of background tasks.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configureAction">The delegate used to configure the worker host options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilder AddWorkerHost(this IServiceCollection services, Action<WorkerHostOptions> configureAction) {
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<IJobFactory, QuartzJobFactory>();
            services.AddTransient<QuartzJobRunner>();
            services.AddTransient<TaskHandlerActivator>();
            services.AddHostedService<WorkerHostedService>();
            var workerHostOptions = new WorkerHostOptions(services);
            configureAction.Invoke(workerHostOptions);
            services.AddSingleton(workerHostOptions.JsonOptions);
            return new WorkerHostBuilder(services, workerHostOptions);
        }

        /// <summary>
        /// Uses Azure Storage service as the store for distributed locking.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <param name="configureAction">Configure the azure options.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseAzureStorageLock(this WorkerHostOptions options, Action<LockManagerAzureOptions> configureAction = null) {
            options.Services.TryAddSingleton(typeof(ILockManager), sp => {
                var azureOptions = new LockManagerAzureOptions {
                    StorageConnection = sp.GetService<IConfiguration>().GetConnectionString("StorageConnection"),
                    EnvironmentName = sp.GetService<IHostEnvironment>().EnvironmentName
                };
                configureAction?.Invoke(azureOptions);
                return new LockManagerAzure(azureOptions);
            });
            return options;
        }

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
        public static WorkerHostOptions UseInMemoryStorage(this WorkerHostOptions options) =>
            throw new NotImplementedException();//options.UseStorage(typeof(WorkItemQueueInMemory<>));

        /// <summary>
        /// Uses a database table, in order to manage queue items. If no <paramref name="configureAction"/> is provided, then SQL Server is used as a default provider.
        /// </summary>
        /// /// <typeparam name="TContext"></typeparam>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <param name="configureAction">The delegate used to configure the database table that contains the background jobs.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseEntityFrameworkStorage<TContext>(this WorkerHostOptions options, Action<DbContextOptionsBuilder> configureAction = null) where TContext : TaskDbContext {
            var isDefaultContext = typeof(TContext) == typeof(TaskDbContext);
            var serviceProvider = options.Services.BuildServiceProvider();
            Action<DbContextOptionsBuilder> sqlServerConfiguration = builder => builder.UseSqlServer(serviceProvider.GetService<IConfiguration>().GetConnectionString("WorkerDb"));
            configureAction = configureAction ?? sqlServerConfiguration;
            options.Services.AddDbContext<TContext>(configureAction);
            options.Services.AddDbContext<LockDbContext>(configureAction);
            if (!isDefaultContext) {
                options.Services.AddScoped<TaskDbContext, TContext>();
            }
            options.ScheduledTaskStoreType = typeof(EFScheduledTaskStore<>);
            options.QueueStoreType = typeof(EFMessageQueue<>);
            options.LockStoreType = typeof(EFLockManager);
            options.Services.AddTransient<ILockManager, EFLockManager>();
            return options;
        }

        /// <summary>
        /// Uses a database table, in order to manage queue items. If no <paramref name="configureAction"/> is provided, then SQL Server is used as a default provider.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <param name="configureAction">The delegate used to configure the database table that contains the background jobs.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseEntityFrameworkStorage(this WorkerHostOptions options, Action<DbContextOptionsBuilder> configureAction = null) => 
            UseEntityFrameworkStorage<TaskDbContext>(options, configureAction);

        /// <summary>
        /// Registers a job that will be processed by the worker host. Usually followed by a <see cref="WithQueueTrigger{TWorkItem}(TaskTriggerBuilder, Action{QueueOptions})"/> call to configure the way that a job is triggered.
        /// </summary>
        /// <typeparam name="TJobHandler">The type of the class that will handle the job. Must have a process function.</typeparam>
        /// <param name="builder">The <see cref="WorkerHostBuilder"/> used to configure the worker host.</param>
        /// <returns>The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</returns>
        public static TaskTriggerBuilder AddJob<TJobHandler>(this WorkerHostBuilder builder) where TJobHandler : class => 
            new TaskTriggerBuilder(builder.Services, builder.Options, typeof(TJobHandler));

        /// <summary>
        /// Specifies that the configured job will be triggered by an item inserted to the a queue.
        /// </summary>
        /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
        /// <param name="configureAction">The delegate used to configure the queue options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilderForQueue WithQueueTrigger<TWorkItem>(this TaskTriggerBuilder builder, Action<QueueOptions> configureAction = null) where TWorkItem : class => 
            WithQueueTrigger<TWorkItem>(builder, builder.Options.QueueStoreType.MakeGenericType(typeof(TWorkItem)), configureAction);

        /// <summary>
        /// Specifies that the configured job will be triggered by an item inserted to the a queue.
        /// </summary>
        /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
        /// <param name="configureAction">The delegate used to configure the queue options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilderForQueue WithQueueTrigger<TWorkItem, TQueueStore>(this TaskTriggerBuilder builder, Action<QueueOptions> configureAction = null) where TWorkItem : class => 
            WithQueueTrigger<TWorkItem>(builder, typeof(TQueueStore), configureAction);

        /// <summary>
        /// Specifies that the configured job will be triggered by an item inserted to the a queue.
        /// </summary>
        /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
        /// <param name="queueStoreTypeImpl">CLR type of the implementation of an <see cref="IMessageQueue{T}"/></param>
        /// <param name="configureAction">The delegate used to configure the queue options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        private static WorkerHostBuilderForQueue WithQueueTrigger<TWorkItem>(this TaskTriggerBuilder builder, Type queueStoreTypeImpl, Action<QueueOptions> configureAction = null) where TWorkItem : class {
            var options = new QueueOptions(builder.Services);
            configureAction?.Invoke(options);
            options.Services.AddTransient(builder.JobHandlerType);
            options.Services.AddTransient(typeof(IQueueNameResolver<TWorkItem>), sp => Activator.CreateInstance(typeof(DefaultQueueNameResolver<TWorkItem>), new object[] { options }));
            options.Services.AddTransient(typeof(IMessageQueue<TWorkItem>), queueStoreTypeImpl);
            var queueStoreTypeDefault = builder.Options.QueueStoreType.MakeGenericType(typeof(TWorkItem));
            if (!queueStoreTypeDefault.Equals(queueStoreTypeImpl)) {
                options.Services.AddTransient(queueStoreTypeDefault);
            }
            options.Services.AddTransient(typeof(DequeueJob<TWorkItem>));
            options.Services.AddTransient(typeof(DequeuedCleanupJob<TWorkItem>));
            options.Services.AddTransient(serviceProvider => new DequeueJobSettings(
                jobHandlerType: builder.JobHandlerType,
                workItemType: typeof(TWorkItem),
                jobName: serviceProvider.GetService<IQueueNameResolver<TWorkItem>>().Resolve(),
                pollingInterval: options.PollingInterval,
                backoffThreshold: options.MaxPollingInterval,
                cleanUpInterval: options.CleanUpInterval,
                cleanUpBatchSize: options.CleanUpBatchSize,
                instanceCount: options.InstanceCount
            ));
            return new WorkerHostBuilderForQueue(options.Services, builder.Options, typeof(TWorkItem));
        }

        /// <summary>
        /// Specifies that the configured job will be triggered by an item inserted to the a queue.
        /// </summary>
        /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
        /// <param name="cronExpression">Corn expressinon</param>
        /// <param name="configureAction">The delegate used to configure the queue options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilder WithScheduleTrigger(this TaskTriggerBuilder builder, string cronExpression, Action<ScheduleOptions> configureAction = null) {
            return WithScheduleTrigger<Dictionary<string, object>>(builder, cronExpression, configureAction);
        }

        /// <summary>
        /// Specifies that the configured job will be triggered by an item inserted to the a queue.
        /// </summary>
        /// <typeparam name="TState">The type of the persisted job state. This is a DTO to share state between every execution</typeparam>
        /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
        /// <param name="cronExpression">Corn expressinon</param>
        /// <param name="configureAction">The delegate used to configure the queue options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilder WithScheduleTrigger<TState>(this TaskTriggerBuilder builder, string cronExpression, Action<ScheduleOptions> configureAction = null) where TState : class {
            if (string.IsNullOrWhiteSpace(cronExpression)) {
                throw new ArgumentException($"'{nameof(cronExpression)}' cannot be null or whitespace", nameof(cronExpression));
            }
            var options = new ScheduleOptions(builder.Services);
            options.CronExpression = cronExpression;
            configureAction?.Invoke(options);
            options.Services.AddTransient(builder.JobHandlerType);
            options.Services.AddTransient(typeof(IScheduledTaskStore<TState>), builder.Options.ScheduledTaskStoreType.MakeGenericType(typeof(TState)));
            options.Services.AddTransient(typeof(ScheduledJob<,>).MakeGenericType(builder.JobHandlerType, typeof(TState)));
            options.Services.AddTransient(serviceProvider => new ScheduledJobSettings(builder.JobHandlerType, typeof(TState), cronExpression, options.Name, options.Group, options.Description));
            return new WorkerHostBuilder(options.Services, builder.Options);
        }

        /// <summary>
        /// Specifies that the configured job will be triggered by an item inserted to the a queue.
        /// </summary>
        /// <typeparam name="TWorkItem"></typeparam>
        /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="pollingIntervalInSeconds">Specifies the time interval between two attempts to dequeue new items.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilder WithQueueTrigger<TWorkItem>(this TaskTriggerBuilder builder, string queueName, int pollingIntervalInSeconds) where TWorkItem : class =>
            builder.WithQueueTrigger<TWorkItem>(options => new QueueOptions(builder.Services) {
                QueueName = queueName,
                PollingInterval = pollingIntervalInSeconds
            });

    }
}
