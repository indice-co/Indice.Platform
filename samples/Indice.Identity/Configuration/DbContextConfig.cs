using Indice.Identity.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        public static IServiceCollection AddDbContextConfig(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment hostingEnvironment) {
            return services.AddEntityFrameworkSqlServer()
                           .AddDbContext<ExtendedIdentityDbContext>((serviceProvider, options) => {
                               options.UseSqlServer(configuration.GetConnectionString("IdentityDb"));
                               if (hostingEnvironment.IsDevelopment()) {
                                   // During development throw an exception when EF query is evaluated on the client.
                                   options.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));
                               }
                           });
        }
    }
}
