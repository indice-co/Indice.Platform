using Indice.Extensions.Configuration.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        public static IHostBuilder UseDatabaseConfiguration<TContext>(this IHostBuilder hostBuilder, Action<EntityConfigurationOptions> configureAction) where TContext : DbContext, IAppSettingsDbContext =>
            hostBuilder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseDatabaseConfiguration<TContext>(configureAction));

        /// <summary>
        /// Registers and configures the <see cref="EntityConfigurationProvider{T}"/> using some default values.
        /// </summary>
        /// <param name="webHostBuilder">A builder for <see cref="IWebHost"/>.</param>
        /// <param name="configureAction">The <see cref="EntityConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IWebHostBuilder UseDatabaseConfiguration<TContext>(this IWebHostBuilder webHostBuilder, Action<EntityConfigurationOptions> configureAction) where TContext : DbContext, IAppSettingsDbContext {
            return webHostBuilder.ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) => {
                var options = new EntityConfigurationOptions {
                    Configuration = configurationBuilder.Build()
                };
                configureAction?.Invoke(options);
                var result = options.Validate();
                if (!result.Succedded) {
                    throw new ArgumentException(result.Error);
                }
                configurationBuilder.Add(new EntityConfigurationSource<TContext>(options));
            });
        }
    }
}
