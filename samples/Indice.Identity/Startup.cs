using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hellang.Middleware.ProblemDetails;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Events;
using Indice.AspNetCore.Identity.Localization;
using Indice.AspNetCore.Middleware;
using Indice.Configuration;
using Indice.Identity.Configuration;
using Indice.Identity.Security;
using Indice.Identity.Services;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Indice.Identity
{
    /// <summary>
    /// Bootstrap class for the application.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Creates a new instance of <see cref="Startup"/>.
        /// </summary>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public Startup(IWebHostEnvironment hostingEnvironment, IConfiguration configuration) {
            HostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Settings = Configuration.GetSection(GeneralSettings.Name).Get<GeneralSettings>();
        }

        /// <summary>
        /// Provides information about the web hosting environment an application is running in.
        /// </summary>
        public IWebHostEnvironment HostingEnvironment { get; }
        /// <summary>
        /// Represents a set of key/value application configuration properties.
        /// </summary>
        public IConfiguration Configuration { get; }
        /// <summary>
        /// General settings for an ASP.NET Core application.
        /// </summary>
        public GeneralSettings Settings { get; }
        /// <summary>
        /// Represents a type used to perform logging.
        /// </summary>
        public ILogger<Startup> Logger { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public void ConfigureServices(IServiceCollection services) {
            // https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core#using-applicationinsightsserviceoptions
            var aiOptions = new ApplicationInsightsServiceOptions();
            // https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core
            services.AddApplicationInsightsTelemetry(aiOptions);
            services.AddMvcConfig(Configuration);
            services.AddLocalization(options => {
                options.ResourcesPath = "Resources";
            });
            services.AddCors(options => options.AddDefaultPolicy(builder => {
                builder.WithOrigins(Configuration.GetSection("AllowedOrigins").Get<string[]>())
                       .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                       .WithHeaders("Authorization", "Content-Type")
                       .WithExposedHeaders("Content-Disposition");
            }));
            services.AddAuthenticationConfig(Configuration);
            services.AddIdentityConfig(Configuration);
            services.AddIdentityServerConfig(HostingEnvironment, Configuration, Settings);
            services.AddProblemDetailsConfig(HostingEnvironment);
            services.ConfigureNonBreakingSameSiteCookies();
            services.AddSmsServiceYubotoOmni(Configuration);
            services.AddEmailServiceSparkpost(Configuration)
                    .WithMvcRazorRendering();
            services.AddSwaggerGen(options => {
                options.IndiceDefaults(Settings);
                options.AddFluentValidationSupport();
                options.AddOAuth2AuthorizationCodeFlow(Settings);
                options.AddFormFileSupport();
                options.AddRequiresOtpOperationFilter();
                options.SchemaFilter<CreateUserRequestSchemaFilter>();
                options.IncludeXmlComments(Assembly.Load(IdentityServerApi.AssemblyName));
            });
#if DEBUG
            services.AddDataProtectionLocal();
#else
            services.AddDataProtectionAzure();
#endif
            services.AddResponseCaching();
            services.AddDataProtectionLocal(options => options.FromConfiguration());
            services.AddClientThemingService();
            services.AddCsp(options => {
                options.ScriptSrc = CSP.Self;
                options.AddSandbox("allow-popups")
                       .AddScriptSrc("https://az416426.vo.msecnd.net")
                       .AddFontSrc(CSP.Data)
                       .AddConnectSrc(CSP.Self)
                       .AddConnectSrc("https://dc.services.visualstudio.com")
                       .AddConnectSrc("https://switzerlandnorth-0.in.applicationinsights.azure.com")
                       .AddFrameAncestors("https://localhost:2002");
            });
            services.AddPlatformEventHandler<DeviceDeletedEvent, DeviceDeletedEventHandler>();
            //services.AddClientIpRestrinctions();
            //services.AddClientIpRestrinctions(options => {
            //    options.StatusCodeOnAccessDenied = System.Net.HttpStatusCode.NotFound;
            //    options.AddIpAddressList("MyWhiteList", "127.0.0.1;192.168.1.5;::1");
            //    options.MapPath("/admin", "192.168.1.5");
            //    options.MapPath("/docs", "MyWhiteList");
            //    options.IgnorePath("/admin", "GET");
            //});
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        public void Configure(IApplicationBuilder app) {
            if (HostingEnvironment.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.IdentityServerStoreSetup<ExtendedConfigurationDbContext>(Clients.Get(), Resources.GetIdentityResources(), Resources.GetApis(), Resources.GetApiScopes());
            } else {
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            //app.UseClientIpRestrictions();
            var staticFileOptions = new StaticFileOptions {
                OnPrepareResponse = context => {
                    const int durationInSeconds = 60 * 60 * 24;
                    context.Context.Response.Headers[HeaderNames.CacheControl] = $"public,max-age={durationInSeconds}";
                    context.Context.Response.Headers.Append(HeaderNames.Expires, DateTime.UtcNow.AddSeconds(durationInSeconds).ToString("R", CultureInfo.InvariantCulture));
                }
            };
            app.UseStaticFiles(staticFileOptions);
            app.UseCookiePolicy();
            app.UseRouting();
            // Use the middleware with parameters to log request responses to the ILogger or use custom parameters to lets say take request response snapshots for testing purposes.
            var useRequestResponseLogging = Configuration.GetValue<bool>($"{nameof(GeneralSettings.Name)}:UseRequestResponseLogging");
            if (useRequestResponseLogging) {
                app.UseRequestResponseLogging((options) => {
                    //options.LogHandler = async (logger, model) => {
                    //    var filename = $"{model.RequestTime:yyyyMMdd.HHmmss}_{model.RequestTarget.Replace('/', '-')}_{model.StatusCode}";
                    //    var folder = Path.Combine(HostingEnvironment.ContentRootPath, @"App_Data\snapshots");
                    //    if (Directory.Exists(folder)) {
                    //        Directory.CreateDirectory(folder);
                    //    }
                    //    if (!string.IsNullOrEmpty(model.RequestBody)) {
                    //        await File.WriteAllTextAsync(Path.Combine(folder, $"{filename}_request.txt"), model.RequestBody);
                    //    }
                    //    if (!string.IsNullOrEmpty(model.ResponseBody)) {
                    //        await File.WriteAllTextAsync(Path.Combine(folder, $"{filename}_request.txt"), model.ResponseBody);
                    //    }
                    //};
                    options.LogHandler = (logger, model) => {
                        // Write response body to App Insights.
                        var requestTelemetry = model.HttpContext.Features.Get<RequestTelemetry>();
                        requestTelemetry?.Properties.Add(nameof(model.ResponseBody), model.ResponseBody);
                        requestTelemetry?.Properties.Add(nameof(model.RequestBody), model.RequestBody);
                        requestTelemetry?.Properties.Add("RequestHeaders", string.Join("\n", model.HttpContext.Request.Headers.Select(x => $"{x.Key}: {x.Value}")));
                        requestTelemetry?.Properties.Add("ResponseHeaders", string.Join("\n", model.HttpContext.Response.Headers.Select(x => $"{x.Key}: {x.Value}")));
                        return Task.CompletedTask;
                    };
                });
            }
            app.UseIdentityServer();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), branch => {
                if (!HostingEnvironment.IsDevelopment()) {
                    branch.UseExceptionHandler("/error");
                }
            });
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), branch => {
                branch.UseProblemDetails();
            });
            app.UseRequestLocalization(new RequestLocalizationOptions {
                DefaultRequestCulture = new RequestCulture(SupportedCultures.Default),
                RequestCultureProviders = new List<IRequestCultureProvider> {
                    new QueryStringRequestCultureProvider(),
                    new QueryStringToCookieRequestCultureProvider { QueryParameterName = "ui_locales" },
                    new CookieRequestCultureProvider()
                },
                SupportedCultures = SupportedCultures.Get().ToList(),
                SupportedUICultures = SupportedCultures.Get().ToList()
            });
            app.UseResponseCaching();
            var enableSwagger = HostingEnvironment.IsDevelopment() || Configuration.GetValue<bool>($"{GeneralSettings.Name}:SwaggerUI");
            if (enableSwagger) {
                app.UseSwaggerUI(options => {
                    options.RoutePrefix = "docs";
                    options.SwaggerEndpoint($"/swagger/{IdentityServerApi.Scope}/swagger.json", IdentityServerApi.Scope);
                    options.OAuth2RedirectUrl($"{Settings.Host}/docs/oauth2-redirect.html");
                    options.OAuthClientId("swagger-ui");
                    options.OAuthAppName("Swagger UI");
                    options.DocExpansion(DocExpansion.None);
                    options.OAuthUsePkce();
                    options.OAuthScopeSeparator(" ");
                    options.EnableDeepLinking();
                });
            }
            app.UseAdminUI(options => {
                options.PathPrefix = "admin";
                options.ClientId = "idsrv-admin-ui";
                options.DocumentTitle = "Admin UI";
                options.Authority = Settings.Authority;
                options.Host = Settings.Host;
                options.PostLogoutRedirectUri = string.Empty;
                options.Enabled = true;
                options.OnPrepareResponse = staticFileOptions.OnPrepareResponse;
                options.InjectStylesheet("/css/admin-ui-overrides.css");
            });
            app.UseEndpoints(endpoints => {
                endpoints.MapSwagger();
                endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
