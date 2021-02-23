using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Indice.Extensions.Configuration.EntityFrameworkCore
{
    /// <summary>
    /// Extension methods for registering <see cref="EFConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class EFConfigurationExtensions
    {
        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the database.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="configureAction">The <see cref="EFConfigurationOptions"/> to use.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddEFConfiguration(this IConfigurationBuilder builder, Action<EFConfigurationOptions> configureAction) {
            return builder.Add(new EFConfigurationSource(configureAction));
        }

        /// <summary>
        /// Configures the <see cref="DbContextOptions"/> used by the <see cref="DbContext"/>.
        /// </summary>
        /// <param name="options">Configuration options for <see cref="EFConfigurationProvider"/>.</param>
        /// <param name="configureAction">The <see cref="DbContextOptions"/> to use.</param>
        public static void ConfigureDbContext(this EFConfigurationOptions options, Action<DbContextOptionsBuilder> configureAction) {
            options.DbContextOptionsBuilder = configureAction;
        }

        /// <summary>
        /// Registers and configures the <see cref="EFConfigurationProvider"/> using some default values.
        /// </summary>
        /// <param name="hostBuilder">A program initialization abstraction.</param>
        /// <param name="reloadInterval">The <see cref="TimeSpan"/> to wait in between each attempt at polling the database for changes. Default is null which indicates no reloading.</param>
        /// <param name="connectionStringName">The name of the connection string to user. Default is 'IdentityDb'</param>
        /// <returns>The <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder UseEFConfiguration(this IHostBuilder hostBuilder, TimeSpan? reloadInterval = null, string connectionStringName = "DefaultConnection") {
            hostBuilder.ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) => {
                var builder = configurationBuilder.Build();
                configurationBuilder.AddEFConfiguration(options => {
                    options.ReloadInterval = reloadInterval;
                    options.ConfigureDbContext(dbContextOptions => {
                        var connectionString = builder.GetConnectionString(connectionStringName);
                        dbContextOptions.UseSqlServer(connectionString);
                    });
                });
            });
            return hostBuilder;
        }
    }
}
