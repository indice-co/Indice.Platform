using System;
using Indice.Extensions;
using Indice.Hosting;
using Indice.Hosting.Data;
using Indice.Hosting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods on <see cref="IServiceCollection"/> that help register required services for writing messages in the task store.
    /// </summary>
    public static class WorkPublisherConfiguration
    {
        /// <summary>
        /// Registers the required services so you can write messages in the task store.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configureAction">The delegate used to configure the work publisher options.</param>
        /// <returns>The <see cref="WorkPublisherBuilder"/> used to configure the work publisher.</returns>
        public static WorkPublisherBuilder AddWorkPublisher(this IServiceCollection services, Action<WorkPublisherOptions> configureAction = null) {
            var workerHostOptions = new WorkPublisherOptions(services) {
                QueueStoreType = typeof(MessageQueueNoop<>)
            };
            configureAction?.Invoke(workerHostOptions);
            services.AddSingleton(workerHostOptions.JsonOptions);
            var builder = new WorkPublisherBuilder(services, workerHostOptions);
            services.AddSingleton(builder);
            return builder;
        }

        /// <summary>
        /// Uses the tables of a relational database in order to manage queue items.
        /// </summary>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <param name="configureAction">The delegate used to configure the database table that contains the background jobs.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkerHostOptions UseStoreRelational(this WorkerHostOptions options, Action<DbContextOptionsBuilder> configureAction = null) => options.UseStoreRelational<TaskDbContext>(configureAction);

        /// <summary>
        /// Uses the tables of a relational database in order to manage queue items.
        /// </summary>
        /// <typeparam name="TContext">The type of <see cref="DbContext"/>.</typeparam>
        /// <param name="options">The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</param>
        /// <param name="configureAction">The delegate used to configure the database table that contains the background jobs.</param>
        /// <returns>The <see cref="WorkerHostOptions"/> used to configure locking and queue persistence.</returns>
        public static WorkPublisherOptions UseStoreRelational<TContext>(this WorkPublisherOptions options, Action<DbContextOptionsBuilder> configureAction = null) where TContext : TaskDbContext {
            var isDefaultContext = typeof(TContext) == typeof(TaskDbContext);
            var connectionString = options.Services.BuildServiceProvider().GetService<IConfiguration>().GetConnectionString("WorkerDb");
            void sqlServerConfiguration(DbContextOptionsBuilder builder) => builder.UseSqlServer(connectionString);
            configureAction ??= sqlServerConfiguration;
            options.Services.AddDbContext<TContext>(configureAction);
            if (!isDefaultContext) {
                options.Services.TryAddScoped<TaskDbContext, TContext>();
            }
            options.QueueStoreType = typeof(MessageQueueRelational<>);
            return options;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TWorkItem"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static WorkPublisherBuilder ForWorkItem<TWorkItem>(this WorkPublisherBuilder builder) where TWorkItem : class =>
            builder.ForWorkItem<TWorkItem>(builder.Options.QueueStoreType?.MakeGenericType(typeof(TWorkItem)), typeof(TWorkItem).Name.ToKebabCase());

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TWorkItem"></typeparam>
        /// <param name="builder"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public static WorkPublisherBuilder ForEvent<TWorkItem>(this WorkPublisherBuilder builder, string queueName) where TWorkItem : class =>
            builder.ForWorkItem<TWorkItem>(builder.Options.QueueStoreType?.MakeGenericType(typeof(TWorkItem)), queueName);

        private static WorkPublisherBuilder ForWorkItem<TWorkItem>(this WorkPublisherBuilder builder, Type messageQueueType, string queueName) where TWorkItem : class {
            if (messageQueueType is null) {
                throw new ArgumentNullException(nameof(messageQueueType), $"You must provide an implementation for the backing store. Use one of the 'UseStoreXXX' methods to configure the builder.");
            }
            var options = new QueueOptions(builder.Services) {
                QueueName = queueName
            };
            builder.Services.AddTransient(typeof(IQueueNameResolver<TWorkItem>), serviceProvider => Activator.CreateInstance(typeof(DefaultQueueNameResolver<TWorkItem>), new object[] { options }));
            builder.Services.AddTransient(typeof(IMessageQueue<TWorkItem>), messageQueueType);
            var messageQueueDefaultType = builder.Options.QueueStoreType.MakeGenericType(typeof(TWorkItem));
            if (!messageQueueDefaultType.Equals(messageQueueType)) {
                builder.Services.TryAddTransient(messageQueueDefaultType);
            }
            return builder;
        }
    }
}
