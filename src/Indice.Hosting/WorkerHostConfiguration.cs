﻿using System.Collections.Specialized;
using Indice.Hosting;
using Indice.Hosting.Data;
using Indice.Hosting.Services;
using Indice.Hosting.Tasks;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="IServiceCollection"/> that help register required services for background task processing.</summary>
public static class WorkerHostConfiguration
{
    /// <summary>Registers a hosted service that manages and configures the lifetime of background tasks.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configureAction">The delegate used to configure the worker host options.</param>
    /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
    public static WorkerHostBuilder AddWorkerHost(this IServiceCollection services, Action<WorkerHostOptions>? configureAction = null) {
        var serviceProvider = services.BuildServiceProvider();
        var builderInstance = serviceProvider.GetService<WorkerHostBuilder>();
        if (builderInstance is not null) {
            return builderInstance;
        }
        var workerHostOptions = new WorkerHostOptions(services) {
            ScheduledTaskStoreType = typeof(ScheduledTaskStoreNoop<>),
            QueueStoreType = typeof(MessageQueueNoop<>),
            LockStoreType = typeof(LockManagerNoop)
        };
        configureAction?.Invoke(workerHostOptions);
        services.AddSingleton(workerHostOptions.JsonOptions);
        // https://www.quartz-scheduler.net/documentation/quartz-3.x/configuration/reference.html
        var quartzConfiguration = new NameValueCollection {
            { "quartz.threadPool.maxConcurrency", "100" },
            { "quartz.threadPool.threadCount", "100" }
        };
        services.AddSingleton<ISchedulerFactory>(serviceProvider => new StdSchedulerFactory(quartzConfiguration));
        services.AddSingleton<IJobFactory, QuartzJobFactory>();
        services.AddTransient<QuartzJobRunner>();
        services.AddTransient<TaskHandlerActivator>();
        services.AddLockManagerNoop();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        if (!configuration.WorkerHostDisabled()) {
            services.AddHostedService<WorkerHostedService>();
        }
        var builder = new WorkerHostBuilder(services, workerHostOptions);
        services.AddSingleton(builder);
        return builder;
    }

    /// <summary>Uses Azure Storage service as the store for distributed locking.</summary>
    /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
    /// <param name="configureAction">Configure the azure options.</param>
    /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
    public static WorkerHostOptions UseLockManagerAzure(this WorkerHostOptions options, Action<IServiceProvider, LockManagerAzureOptions>? configureAction = null) {
        options.Services.AddLockManagerAzure(configureAction);
        return options;
    }

    /// <summary>Manages access to queues using an in-memory mechanism. Not suitable for distributed scenarios.</summary>
    /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
    /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
    public static WorkerHostOptions UseLockManagerInMemory(this WorkerHostOptions options) {
        options.Services.AddLockManagerInMemory();
        return options;
    }

    /// <summary>Registers an implementation of <see cref="ILockManager"/> which is used for distributed locking.</summary>
    /// <typeparam name="TLockManager">The concrete type of <see cref="ILockManager"/> to use.</typeparam>
    /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
    /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
    public static WorkerHostOptions UseLockManager<TLockManager>(this WorkerHostOptions options) where TLockManager : ILockManager {
        options.Services.AddScoped(typeof(ILockManager), typeof(TLockManager));
        return options;
    }

    /// <summary>Registers an implementation of <see cref="ILockManager"/> which is used for distributed locking.</summary>
    /// <typeparam name="TLockManager">The concrete type of <see cref="ILockManager"/> to use.</typeparam>
    /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
    /// <param name="implementationFactory">The factory that creates the service.</param>
    /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
    public static WorkerHostOptions UseLockManager<TLockManager>(this WorkerHostOptions options, Func<IServiceProvider, TLockManager> implementationFactory) where TLockManager : ILockManager {
        options.Services.AddScoped(typeof(ILockManager), serviceProvider => implementationFactory(serviceProvider));
        return options;
    }

    /// <summary>Uses the tables of a relational database in order to manage queue items.</summary>
    /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
    /// <param name="configureAction">The delegate used to configure the database table that contains the background jobs.</param>
    /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
    public static WorkPublisherOptions UseStoreRelational(this WorkPublisherOptions options, Action<DbContextOptionsBuilder>? configureAction = null) => options.UseStoreRelational<TaskDbContext>(configureAction);

    /// <summary>Uses the tables of a relational database in order to manage queue items.</summary>
    /// <typeparam name="TContext">The type of <see cref="DbContext"/>.</typeparam>
    /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
    /// <param name="configureAction">The delegate used to configure the database table that contains the background jobs.</param>
    /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
    public static WorkerHostOptions UseStoreRelational<TContext>(this WorkerHostOptions options, Action<DbContextOptionsBuilder>? configureAction = null) where TContext : TaskDbContext {
        var isDefaultContext = typeof(TContext) == typeof(TaskDbContext);
        var connectionString = options.Services.BuildServiceProvider().GetRequiredService<IConfiguration>().GetConnectionString("WorkerDb");
        void sqlServerConfiguration(DbContextOptionsBuilder builder) => builder.UseSqlServer(connectionString);
        configureAction ??= sqlServerConfiguration;
        options.Services.AddDbContext<TContext>(configureAction);
        options.Services.AddDbContext<LockDbContext>(configureAction);
        if (!isDefaultContext) {
            options.Services.TryAddScoped<TaskDbContext, TContext>();
        }
        options.ScheduledTaskStoreType = typeof(ScheduledTaskStoreEF<>);
        options.QueueStoreType = typeof(MessageQueueRelational<>);
        options.LockStoreType = typeof(LockManagerRelational);
        options.UseLockManager<LockManagerRelational>();
        return options;
    }

    /// <summary>Registers a job that will be processed by the worker host. Usually followed by a <see cref="WithQueueTrigger{TWorkItem}(TaskTriggerBuilder, Action{QueueOptions})"/> call to configure the way that a job is triggered.</summary>
    /// <typeparam name="TJobHandler">The type of the class that will handle the job. Must have a process function.</typeparam>
    /// <param name="builder">The <see cref="WorkerHostBuilder"/> used to configure the worker host.</param>
    /// <returns>The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</returns>
    public static TaskTriggerBuilder AddJob<TJobHandler>(this WorkerHostBuilder builder) where TJobHandler : class => new(builder.Services, builder.Options, typeof(TJobHandler));

    /// <summary>Specifies that the configured job will be triggered by an item inserted to the a queue.</summary>
    /// <typeparam name="TWorkItem">The type of the work item.</typeparam>
    /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="pollingIntervalInSeconds">Specifies the time interval between two attempts to dequeue new items.</param>
    /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
    public static WorkerHostBuilder WithQueueTrigger<TWorkItem>(this TaskTriggerBuilder builder, string queueName, double pollingIntervalInSeconds) where TWorkItem : class =>
        builder.WithQueueTrigger<TWorkItem>(options => new QueueOptions(builder.Services) {
            QueueName = queueName,
            PollingInterval = pollingIntervalInSeconds
        });

    /// <summary>Specifies that the configured job will be triggered by an item inserted to the a queue.</summary>
    /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
    /// <param name="configureAction">The delegate used to configure the queue options.</param>
    /// <returns>The <see cref="WorkerHostBuilderForQueue"/> used to configure the worker host.</returns>
    public static WorkerHostBuilderForQueue WithQueueTrigger<TWorkItem>(this TaskTriggerBuilder builder, Action<QueueOptions>? configureAction = null) where TWorkItem : class =>
        builder.WithQueueTrigger<TWorkItem>(builder.Options?.QueueStoreType?.MakeGenericType(typeof(TWorkItem))!, configureAction);

    /// <summary>Specifies that the configured job will be triggered by an item inserted to the a queue.</summary>
    /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
    /// <param name="configureAction">The delegate used to configure the queue options.</param>
    /// <returns>The <see cref="WorkerHostBuilderForQueue"/> used to configure the worker host.</returns>
    public static WorkerHostBuilderForQueue WithQueueTrigger<TWorkItem, TQueueStore>(this TaskTriggerBuilder builder, Action<QueueOptions>? configureAction = null) where TWorkItem : class =>
        builder.WithQueueTrigger<TWorkItem>(typeof(TQueueStore), configureAction);

    private static WorkerHostBuilderForQueue WithQueueTrigger<TWorkItem>(this TaskTriggerBuilder builder, Type messageQueueType, Action<QueueOptions>? configureAction = null) where TWorkItem : class {
        if (messageQueueType is null) {
            throw new ArgumentNullException(nameof(messageQueueType), $"You must provide an implementation for the backing store. Use one of the 'UseStoreXXX' methods to configure the builder.");
        }
        var options = new QueueOptions(builder.Services);
        configureAction?.Invoke(options);
        options.Services.AddTransient(builder.JobHandlerType!);
        options.Services.AddTransient(typeof(IQueueNameResolver<TWorkItem>), serviceProvider => Activator.CreateInstance(typeof(DefaultQueueNameResolver<TWorkItem>), [options])!);
        options.Services.AddTransient(typeof(IMessageQueue<TWorkItem>), messageQueueType);
        var messageQueueDefaultType = builder.Options!.QueueStoreType!.MakeGenericType(typeof(TWorkItem));
        if (!messageQueueDefaultType.Equals(messageQueueType)) {
            options.Services.TryAddTransient(messageQueueDefaultType);
        }
        options.Services.AddTransient(typeof(DequeueJob<TWorkItem>));
        options.Services.AddTransient(typeof(DequeuedCleanupJob<TWorkItem>));
        options.Services.AddTransient(serviceProvider => new DequeueJobSettings(
            jobHandlerType: builder.JobHandlerType!,
            workItemType: typeof(TWorkItem),
            jobName: serviceProvider.GetRequiredService<IQueueNameResolver<TWorkItem>>().Resolve(),
            pollingInterval: options.PollingInterval,
            backoffThreshold: options.MaxPollingInterval,
            cleanUpInterval: options.CleanUpInterval,
            cleanUpBatchSize: options.CleanUpBatchSize,
            instanceCount: options.InstanceCount
        ));
        return new WorkerHostBuilderForQueue(options.Services, builder.Options, typeof(TWorkItem));
    }

    /// <summary>Specifies that the configured job will be triggered by an item inserted to the a queue.</summary>
    /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
    /// <param name="cronExpression">Cron expression</param>
    /// <param name="configureAction">The delegate used to configure the queue options.</param>
    /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
    public static WorkerHostBuilder WithScheduleTrigger(this TaskTriggerBuilder builder, string cronExpression, Action<ScheduleOptions>? configureAction = null) =>
        builder.WithScheduleTrigger<Dictionary<string, object>>(cronExpression, configureAction);

    /// <summary>Specifies that the configured job will be triggered by an item inserted to the a queue.</summary>
    /// <typeparam name="TState">The type of the persisted job state. This is a DTO to share state between every execution</typeparam>
    /// <param name="builder">The <see cref="TaskTriggerBuilder"/> used to configure the way that a job is triggered.</param>
    /// <param name="cronExpression">Cron expression</param>
    /// <param name="configureAction">The delegate used to configure the queue options.</param>
    /// <returns>The <see cref="WorkerHostBuilder"/> used to configure the worker host.</returns>
    public static WorkerHostBuilder WithScheduleTrigger<TState>(this TaskTriggerBuilder builder, string cronExpression, Action<ScheduleOptions>? configureAction = null) where TState : class {
        if (string.IsNullOrWhiteSpace(cronExpression)) {
            throw new ArgumentException($"'{nameof(cronExpression)}' cannot be null or whitespace", nameof(cronExpression));
        }
        var options = new ScheduleOptions(builder.Services) {
            CronExpression = cronExpression
        };
        configureAction?.Invoke(options);
        options.Services.AddTransient(builder.JobHandlerType!);
        options.Services.AddTransient(typeof(IScheduledTaskStore<TState>), builder.Options!.ScheduledTaskStoreType!.MakeGenericType(typeof(TState)));
        options.Services.AddTransient(typeof(ScheduledJob<,>).MakeGenericType(builder.JobHandlerType!, typeof(TState)));
        options.Services.AddTransient(serviceProvider => new ScheduledJobSettings(builder.JobHandlerType!, typeof(TState), cronExpression, options.Name, options.Group, options.Description, options.Singleton));
        return new WorkerHostBuilder(options.Services, builder.Options);
    }
}
