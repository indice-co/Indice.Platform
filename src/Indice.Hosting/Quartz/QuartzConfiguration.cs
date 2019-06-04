using System;
using System.Collections.Generic;
using System.Text;
using Indice.Hosting.Quartz;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Configuration extension methods for Quartz based jobs
    /// </summary>
    public static class QuartzConfiguration
    {
        /// <summary>
        /// Bootstraps the Quartz services and components required to run jobs in the container
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static QuartzBuilder AddQuartz(this IServiceCollection services) {
            // Add Quartz services
            services.AddSingleton<IJobFactory, JobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<QuartzJobRunner>();
            services.AddHostedService<QuartzHostedService>();
            return new QuartzBuilder(services);
        }

        /// <summary>
        /// Adds a scoped Job
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        /// <param name="builder"></param>
        /// <param name="cronExpression"></param>
        /// <returns></returns>
        public static QuartzBuilder AddJob<TJob>(this QuartzBuilder builder, string cronExpression) where TJob : class, IJob {
            // Add our job
            builder.Services.AddScoped<TJob>();
            builder.Services.AddSingleton(new JobSchedule(
                jobType: typeof(TJob),
                cronExpression: cronExpression));

            return builder;
        }
    }

    /// <summary>
    /// The quartz builder is a helper class that enables fluent configuration for Quartz based Jobs.
    /// </summary>
    public class QuartzBuilder
    {
        /// <summary>
        /// constructs the builder
        /// </summary>
        /// <param name="services"></param>
        public QuartzBuilder(IServiceCollection services) {
            Services = services;
        }

        /// <summary>
        /// The service collection
        /// </summary>
        public IServiceCollection Services { get; }
    }
}
