using Indice.Extensions.Configuration.Database;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>Extension methods on <see cref="IWebHostBuilder"/> interface.</summary>
public static class WebHostBuilderExtensions
{
    /// <summary>Adds database settings feature using the <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IWebHostBuilder AddDatabaseSettings(this IWebHostBuilder builder) =>
        builder.AddDatabaseSettings<ExtendedIdentityDbContext<User, Role>>((options, configuration) => {
            options.ReloadOnInterval = TimeSpan.FromSeconds(30);
            options.ConfigureDbContext = dbBuilder => dbBuilder.UseSqlServer(configuration.GetConnectionString("IdentityDb"));
        });

    /// <summary>Adds database settings feature using the <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="configureAction"></param>
    /// <returns></returns>
    public static IWebHostBuilder AddDatabaseSettings(this IWebHostBuilder builder, Action<EntityConfigurationOptions, IConfiguration> configureAction) =>
        builder.AddDatabaseSettings<ExtendedIdentityDbContext<User, Role>>(configureAction);

    /// <summary>Adds seed information for the <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>. 
    /// Handy when used with database settings that initialization takes place sooner than the Identity Server is configured</summary>
    /// <param name="builder"></param>
    /// <param name="getInitialUsers">Function that gets the initial users</param>
    /// <returns></returns>
    public static IWebHostBuilder AddInitialUsers(this IWebHostBuilder builder, Func<IEnumerable<User>> getInitialUsers) =>
        builder.ConfigureServices(new Action<IServiceCollection>(services =>
            services.TryAddTransient(sp => new ExtendedIdentityDbContextSeedOptions<User> { InitialUsers = getInitialUsers() })
        ));
}
