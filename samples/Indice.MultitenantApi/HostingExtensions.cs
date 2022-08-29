using Indice.MultitenantApi.Data;
using Indice.MultitenantApi.Services;
using Microsoft.EntityFrameworkCore;

namespace Indice.MultitenantApi
{
    internal static class HostingExtensions
    {
        public static WebApplication ConfigureServices(this WebApplicationBuilder builder) {
            var services = builder.Services;
            var configuration = builder.Configuration;
            var hostingEnvironment = builder.Environment;
            services.AddControllers();
            services.AddDbContext<SaasDbContext>(builder => {
                var migrationsAssembly = typeof(Program).Assembly.GetName().Name;
                if (hostingEnvironment.IsDevelopment()) {
                    builder.EnableDetailedErrors();
                    builder.EnableSensitiveDataLogging();
                }
                builder.UseSqlServer(configuration.GetConnectionString("SaasDb"), sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly));
            });
            services.AddMultiTenancy()
                    .FromHeader()
                    .FromRoute()
                    .FromHost()
                    .WithStore<SaasTenantStore>();
            return builder.Build();
        }

        public static WebApplication ConfigurePipeline(this WebApplication app) {
            return app;
        }
    }
}
