using Indice.Extensions.Configuration;
using Indice.Extensions.Configuration.Database;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extensions on the <see cref="IWebHostBuilder"/>
/// </summary>
public static class IdentityServerWebApplicationBuilderExtensions
{
    /// <summary>Registers and configures the <see cref="ExtendedIdentityDbContext{User, Role}"/> as configuration store.</summary>
    /// <param name="webHostBuilder">A builder for <see cref="IWebHost"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseIdentityDatabaseConfiguration(this IWebHostBuilder webHostBuilder) {
        return webHostBuilder.UseIdentityDatabaseConfiguration((options, configuration) => {
            options.ReloadOnInterval = TimeSpan.FromMinutes(1);
            options.ConfigureDbContext = builder => builder.UseSqlServer(configuration.GetConnectionString("IdentityDb"));
        });
    }

    /// <summary>Registers and configures the <see cref="ExtendedIdentityDbContext{User, Role}"/> as configuration store.</summary>
    /// <param name="webHostBuilder">A builder for <see cref="IWebHost"/>.</param>
    /// <param name="configureAction">The <see cref="EntityConfigurationOptions"/> to use.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    public static IWebHostBuilder UseIdentityDatabaseConfiguration(this IWebHostBuilder webHostBuilder, Action<EntityConfigurationOptions, IConfiguration> configureAction) {
        return webHostBuilder.UseDatabaseConfiguration<ExtendedIdentityDbContext<User, Role>>(configureAction);
    }
}
