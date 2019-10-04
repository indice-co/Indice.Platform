using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Hellang.Middleware.ProblemDetails;
using Indice.AspNetCore.Swagger;
using Indice.Configuration;
using Indice.Identity.Configuration;
using Indice.Identity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        public Startup(IHostingEnvironment hostingEnvironment, IConfiguration configuration) {
            HostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Settings = Configuration.GetSection(GeneralSettings.Name).Get<GeneralSettings>();
        }

        /// <summary>
        /// Provides information about the web hosting environment an application is running in.
        /// </summary>
        public IHostingEnvironment HostingEnvironment { get; }
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
            services.AddMvcConfig();
            services.AddCorsConfig(Configuration);
            services.AddCookiePolicyConfig();
            services.AddAuthorizationConfig();
            services.AddDbContextConfig(Configuration, HostingEnvironment);
            services.AddIdentityConfig(Configuration);
            services.AddIdentityServerConfig(HostingEnvironment, Configuration, Settings);
            services.AddAuthenticationConfig();
            services.AddOptions();
            services.AddLogging();
            services.AddDistributedMemoryCache();
            services.AddIndiceServices(Configuration);
            services.AddEmailServiceSparkpost(Configuration);
            services.AddCustomServices();
            services.AddSwaggerGen(options => options.IndiceDefaults(Settings));
            services.AddResponseCaching();
            services.AddCsp(options => {
                options.AddSandbox("allow-popups");
            });
            services.AddProblemDetailsConfig(HostingEnvironment);
            services.AddSpaStaticFiles(options => {
                options.RootPath = "wwwroot/admin-ui";
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        public void Configure(IApplicationBuilder app) {
            if (HostingEnvironment.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.IdentityServerStoreSetup(Clients.Get(), Resources.GetIdentityResources(), Resources.GetApiResources());
            } else {
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"), branch => {
                if (!HostingEnvironment.IsDevelopment()) {
                    branch.UseExceptionHandler("/error");
                }
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
            app.UseIdentityServer();
            app.UseCors();
            app.UseAuthentication();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            app.UseResponseCaching();
            app.UseCookiePolicy();
            app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), branch => {
                branch.UseProblemDetails();
            });
            app.UseSwagger();
            var enableSwagger = HostingEnvironment.IsDevelopment() || Configuration.GetValue<bool>($"{GeneralSettings.Name}:SwaggerUI");
            if (enableSwagger) {
                app.UseSwaggerUI(swaggerOptions => {
                    swaggerOptions.RoutePrefix = "docs";
                    swaggerOptions.SwaggerEndpoint($"/swagger/{IdentityServerApi.Scope}/swagger.json", Settings.Api?.FriendlyName ?? HostingEnvironment.ApplicationName);
                    swaggerOptions.OAuth2RedirectUrl($"{Settings.Host}/docs/oauth2-redirect.html");
                    swaggerOptions.OAuthClientId("swagger-ui");
                    swaggerOptions.OAuthAppName("Swagger UI");
                    swaggerOptions.DocExpansion(DocExpansion.List);
                });
            }
            app.UseMvc(routes => {
                routes.MapRoute("default", "{controller=Welcome}/{action=Index}/{id?}");
            });
            app.UseSpa(builder => {
                builder.Options.SourcePath = "wwwroot/admin-ui";
            });
        }
    }
}
