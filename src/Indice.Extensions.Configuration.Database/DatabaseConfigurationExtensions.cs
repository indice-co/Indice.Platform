using Indice.Extensions.Configuration.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indice.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for registering <see cref="EntityConfigurationProvider{T}"/> with <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class DatabaseConfigurationExtensions
    {
        /// <summary>
        /// Registers and configures the <see cref="EntityConfigurationProvider{T}"/> using some default values.
        /// </summary>
        /// <param name="hostBuilder">A program initialization abstraction.</param>
        /// <param name="configureAction">The <see cref="EntityConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseDatabaseConfiguration<TContext>(this IHostBuilder hostBuilder, Action<EntityConfigurationOptions, IConfiguration> configureAction) where TContext : DbContext, IAppSettingsDbContext =>
            hostBuilder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseDatabaseConfiguration<TContext>(configureAction));

        /// <summary>
        /// Registers and configures the <see cref="EntityConfigurationProvider{T}"/> using some default values.
        /// </summary>
        /// <param name="webHostBuilder">A builder for <see cref="IWebHost"/>.</param>
        /// <param name="configureAction">The <see cref="EntityConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IWebHostBuilder UseDatabaseConfiguration<TContext>(this IWebHostBuilder webHostBuilder, Action<EntityConfigurationOptions, IConfiguration> configureAction) where TContext : DbContext, IAppSettingsDbContext {
            return webHostBuilder.ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) => {
                var options = new EntityConfigurationOptions();
                configureAction?.Invoke(options, configurationBuilder.Build());
                var result = options.Validate();
                if (!result.Succedded) {
                    throw new ArgumentException(result.Error);
                }

                configurationBuilder.Add(new EntityConfigurationSource<TContext>(options));
            })
            .ConfigureServices((context, services) => {
                services.AddTransient<IAppSettingsDbContext, TContext>();
            });
        }
    }
}
