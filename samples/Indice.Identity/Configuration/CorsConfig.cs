using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains methods for configuring the CORS policy.
    /// </summary>
    public static class CorsConfig
    {
        /// <summary>
        /// Defines the CORS policy for the web application.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IServiceCollection AddCorsConfig(this IServiceCollection services, IConfiguration configuration) {
            return services.AddCors(options => options.AddDefaultPolicy(builder => {
                builder.WithOrigins(configuration.GetSection("AllowedHosts").Get<string[]>())
                       .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                       .WithHeaders("Authorization", "Content-Type")
                       .WithExposedHeaders("Content-Disposition");
            }));
        }
    }
}
