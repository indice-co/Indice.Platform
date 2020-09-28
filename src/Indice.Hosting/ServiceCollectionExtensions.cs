using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.Hosting;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;

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
        /// <returns></returns>
        public static BackgroundTasksBuilder AddBackgroundTasks(this IServiceCollection services, Action<BackgroundTasksBuilder> configureAction) {
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddHostedService<QueuedHostedService>();
            var builder = new BackgroundTasksBuilder(services);
            configureAction.Invoke(builder);
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
        public static BackgroundTasksBuilder AddQueue<TWorkItemHandler, TWorkItem>(this BackgroundTasksBuilder builder, Action<QueueOptions> configureAction = null)
            where TWorkItemHandler : WorkItemHandler<TWorkItem>
            where TWorkItem : WorkItem {
            var options = new QueueOptions {
                Services = builder.Services
            };
            configureAction?.Invoke(options);
            options.Services.AddScoped(typeof(DequeueJob<>).MakeGenericType(typeof(TWorkItem)));
            options.Services.AddScoped(typeof(IWorkItemQueue<>).MakeGenericType(typeof(TWorkItem)), typeof(TWorkItem).MakeGenericType(typeof(TWorkItem)));
            var serviceProvider = options.Services.BuildServiceProvider();
            options.Services = null;
            var hostedService = serviceProvider.GetServices<IHostedService>().OfType<QueuedHostedService>().SingleOrDefault();
            var scheduler = hostedService.Scheduler;
            var jobDetails = JobBuilder.Create<DequeueJob<TWorkItem>>()
                                       .WithIdentity(name: options.QueueName, group: JobGroups.InternalJobsGroup)
                                       .Build();
            var jobTrigger = TriggerBuilder.Create()
                                           .WithIdentity(name: TriggerNames.DequeueJobTrigger, group: JobGroups.InternalJobsGroup)
                                           .StartNow()
                                           .WithSimpleSchedule(x => x.WithIntervalInSeconds(options.PollingIntervalInSeconds).RepeatForever())
                                           .Build();
            Task.Run(async () => await scheduler.ScheduleJob(jobDetails, jobTrigger)).ConfigureAwait(false).GetAwaiter().GetResult();
            return builder;
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
