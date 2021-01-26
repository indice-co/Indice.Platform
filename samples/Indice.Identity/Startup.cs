using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Hellang.Middleware.ProblemDetails;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Features;
using Indice.AspNetCore.Swagger;
using Indice.Configuration;
using Indice.Identity.Configuration;
using Indice.Identity.Hosting;
using Indice.Identity.Security;
using Indice.Identity.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public void ConfigureServices(IServiceCollection services) {
            services.AddMvcConfig(Configuration);
            services.AddLocalization(options => options.ResourcesPath = "Resources");
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
            services.AddSmsServiceYouboto(Configuration);
            services.AddSwaggerGen(options => {
                options.IndiceDefaults(Settings);
                options.AddOAuth2(Settings);
                options.SchemaFilter<CreateUserRequestSchemaFilter>();
                options.IncludeXmlComments(Assembly.Load(IdentityServerApi.AssemblyName));
            });
            services.AddMessageDescriber<ExtendedMessageDescriber>();
            services.AddResponseCaching();
            services.AddDataProtectionLocal(options => options.FromConfiguration());
            services.AddEmailService(Configuration);
            services.AddSmsServiceApifon(Configuration);
            services.AddCsp(options => {
                options.ScriptSrc = CSP.Self;
                options.AddSandbox("allow-popups")
                       .AddFontSrc(CSP.Data)
                       .AddConnectSrc(CSP.Self)
                       .AddConnectSrc("https://dc.services.visualstudio.com")
                       .AddFrameAncestors("https://localhost:2002");
            });
            // Setup worker host for executing background tasks.
            services.AddWorkerHost(options => {
                options.JsonOptions.JsonSerializerOptions.WriteIndented = true;
                options.UseEntityFrameworkStorage<ExtendedTaskDbContext>();
            })
            .AddJob<SMSAlertHandler>()
            .WithQueueTrigger<SMSDto>(options => {
                options.QueueName = "user-messages";
                options.PollingInterval = 500;
            })
            .AddJob<LoadAvailableAlertsHandler>()
            .WithScheduleTrigger<DemoCounterModel>("0/5 * * * * ?", options => {
                options.Name = "load-available-alerts";
                options.Description = "Load alerts for the queue.";
                options.Group = "indice";
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        /// <param name="serviceProvider"></param>
        public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider) {
            if (HostingEnvironment.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.IdentityServerStoreSetup<ExtendedConfigurationDbContext>(Clients.Get(), Resources.GetIdentityResources(), Resources.GetApis(), Resources.GetApiScopes());
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
            app.UseCookiePolicy();
            app.UseRouting();
            // Use the middleware with parameters to log request responses to the ILogger or use custom parameters to lets say take request response snapshots for testing purposes.
            app.UseRequestResponseLogging(
                new[] { MediaTypeNames.Application.Json, MediaTypeNames.Text.Html }, async (logger, model) => {
                    var filename = $"{model.RequestTime:yyyyMMdd.HHmmss}_{model.RequestTarget.Replace('/', '-')}_{model.StatusCode}";
                    var folder = Path.Combine(HostingEnvironment.ContentRootPath, @"App_Data\snapshots");
                    if (!Directory.Exists(folder)) {
                        Directory.CreateDirectory(folder);
                    }
                    if (!string.IsNullOrEmpty(model.RequestBody)) {
                        await File.WriteAllTextAsync(Path.Combine(folder, $"{filename}_request.txt"), model.RequestBody);
                    }
                    await File.WriteAllTextAsync(Path.Combine(folder, $"{filename}_response.txt"), model.ResponseBody);
                }
            );
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
                    new CookieRequestCultureProvider()
                },
                SupportedCultures = SupportedCultures.Get().ToList(),
                SupportedUICultures = SupportedCultures.Get().ToList()
            });
            app.UseResponseCaching();
            app.UseSwagger();
            var enableSwagger = HostingEnvironment.IsDevelopment() || Configuration.GetValue<bool>($"{GeneralSettings.Name}:SwaggerUI");
            if (enableSwagger) {
                app.UseSwaggerUI(options => {
                    options.RoutePrefix = "docs";
                    options.SwaggerEndpoint($"/swagger/{IdentityServerApi.Scope}/swagger.json", IdentityServerApi.Scope);
                    options.OAuth2RedirectUrl($"{Settings.Host}/docs/oauth2-redirect.html");
                    options.OAuthClientId("swagger-ui");
                    options.OAuthClientSecret("M2YwNTlkMTgtYWQzNy00MGNjLWFiYjQtZWQ3Y2Y4N2M3YWU3");
                    options.OAuthAppName("Swagger UI");
                    options.DocExpansion(DocExpansion.None);
                    options.OAuthUsePkce();
                    options.OAuthScopeSeparator(" ");
                });
            }
            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
