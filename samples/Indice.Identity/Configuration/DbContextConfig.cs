using Indice.AspNetCore.Identity.Features;
using Indice.AspNetCore.Identity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains methods to configure the various DbContexts of the application.
    /// </summary>
    public static class DbContextConfig
    {
        /// <summary>
        /// Configures the DbContext of the application and the services required for Microsoft SQL Server database provider.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddDbContextConfig(this IServiceCollection services, IConfiguration configuration) {
            return services.AddEntityFrameworkSqlServer()
                           .AddDbContext<ExtendedIdentityDbContext<User, Role>>((serviceProvider, options) => {
                               options.UseSqlServer(configuration.GetConnectionString("IdentityDb"));
                           });
        }
    }
}
