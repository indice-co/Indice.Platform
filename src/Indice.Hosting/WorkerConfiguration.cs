using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Indice.Hosting;
using Indice.Hosting.Data;
using Indice.Hosting.EntityFrameworkCore;
using Indice.Hosting.Postgres;
using Indice.Hosting.SqlServer;
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
            var workerHostOptions = new WorkerHostOptions(services);
            configureAction.Invoke(workerHostOptions);
            services.AddSingleton(workerHostOptions.JsonOptions);
            var quartzConfiguration = new NameValueCollection {
                { "quartz.threadPool.maxConcurrency", "100" }
            };
            services.AddSingleton<ISchedulerFactory>(serviceProvider => new StdSchedulerFactory(quartzConfiguration));
            services.AddSingleton<IJobFactory, QuartzJobFactory>();
            services.AddTransient<QuartzJobRunner>();
            services.AddTransient<TaskHandlerActivator>();
            services.AddHostedService<WorkerHostedService>();
            return new WorkerHostBuilder(services, workerHostOptions);
        }

        /// <summary>
        /// Uses Azure Storage service as the store for distributed locking.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <param name="configureAction">Configure the azure options.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseAzureStorageLock(this WorkerHostOptions options, Action<LockManagerAzureOptions> configureAction = null) {
            options.Services.TryAddSingleton(typeof(ILockManager), serviceProvider => {
                var azureOptions = new LockManagerAzureOptions {
                    StorageConnection = serviceProvider.GetService<IConfiguration>().GetConnectionString("StorageConnection"),
                    EnvironmentName = serviceProvider.GetService<IHostEnvironment>().EnvironmentName
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
            throw new NotImplementedException();

        /// <summary>
        /// Uses a database table, in order to manage queue items. If no <paramref name="configureAction"/> is provided, then SQL Server is used as a default provider.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <param name="configureAction">The delegate used to configure the database table that contains the background jobs.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseEntityFrameworkStorage(this WorkerHostOptions options, Action<DbContextOptionsBuilder> configureAction = null) =>
            UseEntityFrameworkStorage<TaskDbContext>(options, configureAction);

        /// <summary>
        /// Uses a database table, in order to manage queue items. The underlying database access is managed by Entity Framework. 
        /// If no <paramref name="configureAction"/> is provided, then SQL Server is used as a default provider.
        /// </summary>
        /// /// <typeparam name="TContext"></typeparam>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <param name="configureAction">The delegate used to configure the database table that contains the background jobs.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseEntityFrameworkStorage<TContext>(this WorkerHostOptions options, Action<DbContextOptionsBuilder> configureAction = null) where TContext : TaskDbContext {
            var isDefaultContext = typeof(TContext) == typeof(TaskDbContext);
            var serviceProvider = options.Services.BuildServiceProvider();
            Action<DbContextOptionsBuilder> sqlServerConfiguration = builder => builder.UseSqlServer(serviceProvider.GetService<IConfiguration>().GetConnectionString("WorkerDb"));
            configureAction ??= sqlServerConfiguration;
            options.Services.AddDbContext<TContext>(configureAction);
            options.Services.AddDbContext<LockDbContext>(configureAction);
            if (!isDefaultContext) {
                options.Services.AddScoped<TaskDbContext, TContext>();
            }
            options.ScheduledTaskStoreType = typeof(EFScheduledTaskStore<>);
            options.QueueStoreType = typeof(EFMessageQueue<>);
            options.LockStoreType = typeof(SqlServerLockManager);
            options.Services.AddTransient<ILockManager, SqlServerLockManager>();
            return options;
        }

        /// <summary>
        /// Uses a database table, in order to manage queue items. The underlying database access is managed by SQL Server.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseSqlServerStorage(this WorkerHostOptions options) {
            options.ScheduledTaskStoreType = typeof(EFScheduledTaskStore<>);
            options.QueueStoreType = typeof(SqlServerMessageQueue<>);
            var serviceProvider = options.Services.BuildServiceProvider();
            Action<DbContextOptionsBuilder> sqlServerConfiguration = builder => builder.UseSqlServer(serviceProvider.GetService<IConfiguration>().GetConnectionString("WorkerDb"));
            options.Services.AddDbContext<TaskDbContext>(sqlServerConfiguration);
            options.Services.AddScoped<IDbConnectionFactory>(x => new SqlServerConnectionFactory(serviceProvider.GetService<IConfiguration>().GetConnectionString("WorkerDb")));
            return options;
        }

        /// <summary>
        /// Uses a database table, in order to manage queue items. The underlying database access is managed by PostgreSQL.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UsePostgresStorage(this WorkerHostOptions options) {
            options.ScheduledTaskStoreType = typeof(EFScheduledTaskStore<>);
            options.QueueStoreType = typeof(PostgresMessageQueue<>);
            var serviceProvider = options.Services.BuildServiceProvider();
            Action<DbContextOptionsBuilder> sqlServerConfiguration = builder => builder.UseNpgsql(serviceProvider.GetService<IConfiguration>().GetConnectionString("WorkerDb"));
            options.Services.AddDbContext<TaskDbContext>(sqlServerConfiguration);
            options.Services.AddScoped<IDbConnectionFactory>(x => new PostgresConnectionFactory(serviceProvider.GetService<IConfiguration>().GetConnectionString("WorkerDb")));
            return options;
        }

        /// <summary>
        /// Registers a job that will be processed by the worker host. Usually followed by a <see cref="WithQueueTrigger{TWorkItem}(TaskTriggerBuilder, Action{QueueOptions})"/> call to configure the way that a job is triggered.
        /// </summary>
        /// <typeparam name="TJobHandler">The type of the class that will handle the job. Must have a process function.</typeparam>
        /// <param name="builder">The <see cref="WorkerHostBuilder"/> used to configure the worker host.</param>
        /// <returns>The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</returns>
        public static TaskTriggerBuilder AddJob<TJobHandler>(this WorkerHostBuilder builder) where TJobHandler : class => new(builder.Services, builder.Options, typeof(TJobHandler));

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
        /// <param name="queueStoreTypeImplementation">CLR type of the implementation of an <see cref="IMessageQueue{T}"/></param>
        /// <param name="configureAction">The delegate used to configure the queue options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        private static WorkerHostBuilderForQueue WithQueueTrigger<TWorkItem>(this TaskTriggerBuilder builder, Type queueStoreTypeImplementation, Action<QueueOptions> configureAction = null) where TWorkItem : class {
            var options = new QueueOptions(builder.Services);
            configureAction?.Invoke(options);
            options.Services.AddTransient(builder.JobHandlerType);
            options.Services.AddTransient(typeof(IQueueNameResolver<TWorkItem>), sp => Activator.CreateInstance(typeof(DefaultQueueNameResolver<TWorkItem>), new object[] { options }));
            options.Services.AddTransient(typeof(IMessageQueue<TWorkItem>), queueStoreTypeImplementation);
            var queueStoreTypeDefault = builder.Options.QueueStoreType.MakeGenericType(typeof(TWorkItem));
            if (!queueStoreTypeDefault.Equals(queueStoreTypeImplementation)) {
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
        /// <param name="cronExpression">Cron expression</param>
        /// <param name="configureAction">The delegate used to configure the queue options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilder WithScheduleTrigger(this TaskTriggerBuilder builder, string cronExpression, Action<ScheduleOptions> configureAction = null) =>
            WithScheduleTrigger<Dictionary<string, object>>(builder, cronExpression, configureAction);

        /// <summary>
        /// Specifies that the configured job will be triggered by an item inserted to the a queue.
        /// </summary>
        /// <typeparam name="TState">The type of the persisted job state. This is a DTO to share state between every execution</typeparam>
        /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
        /// <param name="cronExpression">Cron expression</param>
        /// <param name="configureAction">The delegate used to configure the queue options.</param>
        /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
        public static WorkerHostBuilder WithScheduleTrigger<TState>(this TaskTriggerBuilder builder, string cronExpression, Action<ScheduleOptions> configureAction = null) where TState : class {
            if (string.IsNullOrWhiteSpace(cronExpression)) {
                throw new ArgumentException($"'{nameof(cronExpression)}' cannot be null or whitespace", nameof(cronExpression));
            }
            var options = new ScheduleOptions(builder.Services) {
                CronExpression = cronExpression
            };
            configureAction?.Invoke(options);
            options.Services.AddTransient(builder.JobHandlerType);
            options.Services.AddTransient(typeof(IScheduledTaskStore<TState>), builder.Options.ScheduledTaskStoreType.MakeGenericType(typeof(TState)));
            options.Services.AddTransient(typeof(ScheduledJob<,>).MakeGenericType(builder.JobHandlerType, typeof(TState)));
            options.Services.AddTransient(serviceProvider => new ScheduledJobSettings(builder.JobHandlerType, typeof(TState), cronExpression, options.Name, options.Group, options.Description));
            return new WorkerHostBuilder(options.Services, builder.Options);
        }
    }
}
