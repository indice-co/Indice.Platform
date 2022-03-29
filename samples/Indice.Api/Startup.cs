using System;
using System.Globalization;
using System.Linq;
using Indice.Api.Data;
using Indice.AspNetCore.Features.Campaigns;
using Indice.AspNetCore.Features.Campaigns.UI;
using Indice.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Indice.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment) {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            HostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            Settings = Configuration.GetSection(GeneralSettings.Name).Get<GeneralSettings>();
        }

        public IConfiguration Configuration { get; }
        public GeneralSettings Settings { get; }
        public IWebHostEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddMvcConfig(Configuration);
            services.AddCorsConfig(Configuration)
                    .AddSwaggerConfig(Settings)
                    .AddDistributedMemoryCache()
                    .AddAuthenticationConfig(Settings)
                    .AddDbContext<ApiDbContext>(builder => {
                        builder.UseSqlServer(Configuration.GetConnectionString("SettingsDb"));
                    });
            services.AddWorkerHostConfig(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app) {
            if (HostingEnvironment.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            var staticFileOptions = new StaticFileOptions {
                OnPrepareResponse = context => {
                    const int durationInSeconds = 60 * 60 * 24;
                    context.Context.Response.Headers[HeaderNames.CacheControl] = $"public,max-age={durationInSeconds}";
                    context.Context.Response.Headers.Append(HeaderNames.Expires, DateTime.UtcNow.AddSeconds(durationInSeconds).ToString("R", CultureInfo.InvariantCulture));
                }
            };
            app.UseStaticFiles(staticFileOptions);
            app.UseHttpsRedirection();
            app.UseCors();
            app.UseRouting();
            app.UseResponseCaching();
            app.UseAuthentication();
            app.UseAuthorization();
            if (Configuration.EnableSwaggerUi()) {
                app.UseSwaggerUI(options => {
                    options.RoutePrefix = "docs";
                    options.SwaggerEndpoint($"/swagger/{CampaignsApi.Scope}/swagger.json", CampaignsApi.Scope);
                    options.SwaggerEndpoint($"/swagger/lookups/swagger.json", "lookups");
                    options.OAuth2RedirectUrl($"{Settings.Host}/docs/oauth2-redirect.html");
                    options.OAuthClientId("swagger-ui");
                    options.OAuthAppName("Swagger UI");
                    options.DocExpansion(DocExpansion.List);
                    options.OAuthUsePkce();
                    options.OAuthScopeSeparator(" ");
                });
            }
            app.UseCampaignsUI(options => {
                options.Path = "campaigns";
                options.ClientId = "backoffice-ui";
                options.Scope = "backoffice backoffice:campaigns";
                options.DocumentTitle = "Campaigns UI";
                options.Authority = Settings.Authority;
                options.Host = Settings.Host;
                options.Enabled = true;
                options.OnPrepareResponse = staticFileOptions.OnPrepareResponse;
                options.InjectStylesheet("/css/campaigns-ui-overrides.css");
            });
            app.UseEndpoints(endpoints => {
                endpoints.MapSwagger();
                endpoints.MapControllers();
            });
        }
    }
}
