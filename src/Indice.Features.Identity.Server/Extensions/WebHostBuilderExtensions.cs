using Indice.Extensions.Configuration.Database;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>Extension methods on <see cref="IWebHostBuilder"/> interface.</summary>
public static class WebHostBuilderExtensions
{
    /// <summary>Adds database settings feature using the <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</summary>
    /// <param name="builder"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IWebHostBuilder AddDatabaseSettings(this IWebHostBuilder builder, IConfiguration configuration) =>
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
}
