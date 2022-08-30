using Indice.AspNetCore.Middleware;
using Indice.Configuration;
using Indice.MultitenantApi.Data;
using Indice.MultitenantApi.Services;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;

namespace Indice.MultitenantApi
{
    internal static class HostingExtensions
    {
        public static WebApplication ConfigureServices(this WebApplicationBuilder builder) {
            var services = builder.Services;
            var configuration = builder.Configuration;
            var hostingEnvironment = builder.Environment;
            services.AddControllers();
            var migrationsAssembly = typeof(Program).Assembly.GetName().Name;
            services.AddDbContext<SaasDbContext>(builder => {
                if (hostingEnvironment.IsDevelopment()) {
                    builder.EnableDetailedErrors();
                    builder.EnableSensitiveDataLogging();
                }
                builder.UseSqlServer(configuration.GetConnectionString("SaasDb"), sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly));
            });
            services.AddDbContext<ApiDbContext>((sp, builder) => {
                if (hostingEnvironment.IsDevelopment()) {
                    builder.EnableDetailedErrors();
                    builder.EnableSensitiveDataLogging();
                }
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var tenant = httpContextAccessor.HttpContext?.GetTenant();
                builder.UseSqlServer(tenant?.ConnectionString ?? configuration.GetConnectionString("ApiDb"), sqlServerOptions => sqlServerOptions.MigrationsAssembly(migrationsAssembly));
            });
            services.AddMultiTenancy()
                    .FromHeader()
                    .FromRoute()
                    .FromHost()
                    .WithStore<SaasTenantStore>();
            return builder.Build();
        }

        public static WebApplication ConfigurePipeline(this WebApplication app) {
            var configuration = app.Configuration;
            var hostingEnvironment = app.Environment;
            var generalSettings = app.Configuration.Get<IOptions<GeneralSettings>>()?.Value;
            if (hostingEnvironment.IsProduction()) {
                if (configuration.UseRedirectToHost()) {
                    var rewrite = new RewriteOptions();
                    rewrite.Rules.Add(new RedirectToHostRewriteRule(generalSettings.Host));
                    app.UseRewriter(rewrite);
                }
                if (configuration.UseHttpsRedirection()) {
                    app.UseHttpsRedirection();
                }
                if (configuration.HstsEnabled()) {
                    app.UseHsts();
                }
            }
            app.UseRouting();
            app.UseMultiTenancy();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
            return app;
        }
    }
}
