using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CorsConfig
    {
        public static IServiceCollection AddCorsConfig(this IServiceCollection services, IConfiguration configuration) {
            services.AddCors(options => options.AddDefaultPolicy(builder => {
                builder.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>())
                       .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                       .WithHeaders("Authorization", "Content-Type")
                       .WithExposedHeaders("Content-Disposition")
                       .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            }));
            return services;
        }
    }
}
