using System;
using System.Globalization;
using System.Linq;
using Hellang.Middleware.ProblemDetails;
using Indice.Api.Data;
using Indice.AspNetCore.Middleware;
using Indice.Configuration;
using Indice.Features.Messages.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
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
                    .AddSecurityHeaders()
                    .AddSwaggerConfig(Settings)
                    .AddProblemDetailsConfig(HostingEnvironment)
                    .AddDistributedMemoryCache()
                    .AddAuthenticationConfig(Settings)
                    .AddDbContext<ApiDbContext>(builder => {
                        builder.UseSqlServer(Configuration.GetConnectionString("SettingsDb"));
                    });
            services.AddWorkPublisherConfig(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app) {
            if (HostingEnvironment.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                if (Configuration.UseRedirectToHost()) {
                    var rewrite = new RewriteOptions();
                    rewrite.Rules.Add(new RedirectToHostRewriteRule(Settings.Host));
                    app.UseRewriter(rewrite);
                }
                if (Configuration.UseHttpsRedirection()) {
                    app.UseHttpsRedirection();
                }
                if (Configuration.HstsEnabled()) {
                    app.UseHsts();
                }
            }
            var staticFileOptions = new StaticFileOptions {
                OnPrepareResponse = context => {
                    const int durationInSeconds = 60 * 60 * 24;
                    context.Context.Response.Headers[HeaderNames.CacheControl] = $"public,max-age={durationInSeconds}";
                    context.Context.Response.Headers.Append(HeaderNames.Expires, DateTime.UtcNow.AddSeconds(durationInSeconds).ToString("R", CultureInfo.InvariantCulture));
                }
            };
            app.UseStaticFiles(staticFileOptions);
            app.UseCors();
            app.UseRouting();
            app.UseResponseCaching();
            app.UseProblemDetails();
            app.UseAuthentication();
            app.UseAuthorization();
            if (Configuration.EnableSwaggerUi()) {
                app.UseWhen(httpContext => httpContext.Request.Path.StartsWithSegments("/docs"), docsBuilder => {
                    docsBuilder.UseSecurityHeaders(policy => {
                        var csp = policy.ContentSecurityPolicy
                                        .AddScriptSrc(CSP.UnsafeInline)
                                        .AddConnectSrc(CSP.Self)
                                        .AddConnectSrc(Settings.Authority)
                                        .AddSandbox("allow-popups");
                        if (HostingEnvironment.IsDevelopment()) {
                            csp.AddConnectSrc("wss:");
                        }
                    });
                });
                app.UseSwaggerUI(options => {
                    options.RoutePrefix = "docs";
                    options.SwaggerEndpoint($"/swagger/{MessagesApi.Scope}/swagger.json", MessagesApi.Scope);
                    options.SwaggerEndpoint($"/swagger/lookups/swagger.json", "lookups");
                    options.OAuth2RedirectUrl($"{Settings.Host}/docs/oauth2-redirect.html");
                    options.OAuthClientId("swagger-ui");
                    options.OAuthAppName("Swagger UI");
                    options.DocExpansion(DocExpansion.None);
                    options.OAuthUsePkce();
                    options.OAuthScopeSeparator(" ");
                });
            }
            app.UseWhen(httpContext => httpContext.Request.Path.StartsWithSegments("/messages"), messagesBuilder => {
                messagesBuilder.UseSecurityHeaders(policy => {
                    var csp = policy.ContentSecurityPolicy
                                    .AddScriptSrc(CSP.UnsafeEval)
                                    .AddScriptSrc(CSP.UnsafeInline)
                                    .AddConnectSrc(CSP.Self)
                                    .AddConnectSrc(Settings.Authority)
                                    .AddFrameSrc(Settings.Authority)
                                    .AddFrameSrc(CSP.Self)
                                    .AddFrameSrc(CSP.Data) // check this out for <iframe src="data:text/html....">
                                    .AddFrameAncestors(CSP.Self)
                                    .AddSandbox("allow-popups")
                                    .AddFontSrc(CSP.Data);
                    if (HostingEnvironment.IsDevelopment()) {
                        csp.AddConnectSrc("wss:");
                    }
                });
            });
            app.UseCampaignsUI(options => {
                options.Path = "messages";
                options.ClientId = "backoffice-ui";
                options.Scope = "backoffice backoffice:messages";
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
