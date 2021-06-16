using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Indice.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for registering <see cref="EntityConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class EntityConfigurationExtensions
    {
        internal static IConfigurationBuilder AddEntityConfiguration(this IConfigurationBuilder builder, EntityConfigurationOptions options) => builder.Add(new EntityConfigurationSource(options));

        /// <summary>
        /// Registers and configures the <see cref="EntityConfigurationProvider"/> using some default values.
        /// </summary>
        /// <param name="hostBuilder">A program initialization abstraction.</param>
        /// <param name="configureAction">The <see cref="EntityConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseEntityConfiguration(this IHostBuilder hostBuilder, Action<EntityConfigurationOptions> configureAction) =>
            hostBuilder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseEntityConfiguration(configureAction));

        /// <summary>
        /// Registers and configures the <see cref="EntityConfigurationProvider"/> using some default values.
        /// </summary>
        /// <param name="webHostBuilder">A builder for <see cref="IWebHost"/>.</param>
        /// <param name="configureAction">The <see cref="EntityConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IWebHostBuilder UseEntityConfiguration(this IWebHostBuilder webHostBuilder, Action<EntityConfigurationOptions> configureAction) {
            return webHostBuilder.ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) => {
                var options = new EntityConfigurationOptions {
                    Configuration = configurationBuilder.Build()
                };
                configureAction?.Invoke(options);
                var result = options.Validate();
                if (!result.Succedded) {
                    throw new ArgumentException(result.Error);
                }
                configurationBuilder.AddEntityConfiguration(options);
            });
        }
    }
}
