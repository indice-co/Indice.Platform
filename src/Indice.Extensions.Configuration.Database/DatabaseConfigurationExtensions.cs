using Indice.Extensions.Configuration.Database;
using Indice.Extensions.Configuration.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>Extension methods for registering <see cref="EntityConfigurationProvider{T}"/> with <see cref="IConfigurationBuilder"/>.</summary>
public static class DatabaseConfigurationExtensions
{
    /// <summary>Registers and configures the <see cref="EntityConfigurationProvider{T}"/> using some default values.</summary>
    /// <param name="webHostBuilder">A builder for <see cref="IWebHost"/>.</param>
    /// <param name="configureAction">The <see cref="EntityConfigurationOptions"/> to use.</param>
    /// <returns>The <see cref="IHostBuilder"/>.</returns>
    public static IWebHostBuilder AddDatabaseSettings<TContext>(this IWebHostBuilder webHostBuilder, Action<EntityConfigurationOptions, IConfiguration> configureAction) where TContext : DbContext, IAppSettingsDbContext =>
       webHostBuilder.ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) => {
           var options = new EntityConfigurationOptions();
           configureAction?.Invoke(options, configurationBuilder.Build());
           var result = options.Validate();
           if (!result.Succedded) {
               throw new ArgumentException(result.Error);
           }
           configurationBuilder.Add(new EntityConfigurationSource<TContext>(options));
       })
       .ConfigureServices((context, services) => {
           if (typeof(TContext).Equals(typeof(DefaultAppSettingsDbContext))) {
               var options = new EntityConfigurationOptions();
               configureAction?.Invoke(options, context.Configuration);
               services.AddDbContext<TContext>(options.ConfigureDbContext);
           }
           services.AddTransient<IAppSettingsDbContext, TContext>();
       });

    /// <summary>Registers and configures the <see cref="EntityConfigurationProvider{T}"/> using some default values. These are A database connection string named <strong>AppSettingsDb</strong> and a refresh interval of <strong>30 secconds</strong>.</summary>
    /// <param name="webHostBuilder">A builder for <see cref="IWebHost"/>.</param>
    /// <param name="configureAction">The <see cref="EntityConfigurationOptions"/> to use.</param>
    /// <returns>The <see cref="IHostBuilder"/>.</returns>
    /// <remarks>The database will not create the table <strong>[config].[AppSetting]</strong> by itself. Using the internal DbContext for app settings means that we need to manually create the table and schema in our target database</remarks>
    public static IWebHostBuilder AddDatabaseSettingsDefaults(this IWebHostBuilder webHostBuilder, Action<EntityConfigurationOptions, IConfiguration>? configureAction = null) => 
        AddDatabaseSettings<DefaultAppSettingsDbContext>(webHostBuilder, configureAction ?? new Action<EntityConfigurationOptions, IConfiguration>((options, configuration) => {
            options.ReloadOnInterval = TimeSpan.FromSeconds(30);
            options.ConfigureDbContext = dbBuilder => dbBuilder.UseSqlServer(configuration.GetConnectionString("AppSettingsDb"));
        }));
}
