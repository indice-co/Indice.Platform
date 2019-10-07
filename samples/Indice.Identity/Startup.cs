using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hellang.Middleware.ProblemDetails;
using Indice.AspNetCore.Identity.Features;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Swagger;
using Indice.Configuration;
using Indice.Identity.Configuration;
using Indice.Identity.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            services.AddMvcConfig();
            services.AddCors(options => options.AddDefaultPolicy(builder => {
                builder.WithOrigins(Configuration.GetSection("AllowedHosts").Get<string[]>())
                       .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                       .WithHeaders("Authorization", "Content-Type")
                       .WithExposedHeaders("Content-Disposition");
            }));
            services.Configure<CookiePolicyOptions>(options => {
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddDbContext<ExtendedIdentityDbContext<User, Role>>((serviceProvider, options) => {
                options.UseSqlServer(Configuration.GetConnectionString("IdentityDb"));
            });
            services.AddIdentityConfig(Configuration);
            services.AddIdentityServerConfig(HostingEnvironment, Configuration, Settings);
            services.ConfigureApplicationCookie(options => {
                options.AccessDeniedPath = new PathString("/access-denied");
                options.LoginPath = new PathString("/login");
                options.LogoutPath = new PathString("/logout");
            });
            services.AddOptions();
            services.AddLogging();
            services.AddDistributedMemoryCache();
            services.AddIndiceServices(Configuration);
            services.AddEmailServiceSparkpost(Configuration);
            services.AddSwaggerGen(options => {
                options.IndiceDefaults(Settings);
                options.IncludeXmlComments(Assembly.Load($"Indice.AspNetCore.Identity"));
            });
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
            app.UseRouting();
            app.UseIdentityServer();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
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
                    swaggerOptions.SwaggerEndpoint($"/swagger/{IdentityServerApi.Scope}/swagger.json", IdentityServerApi.Scope);
                    swaggerOptions.OAuth2RedirectUrl($"{Settings.Host}/docs/oauth2-redirect.html");
                    swaggerOptions.OAuthClientId("swagger-ui");
                    swaggerOptions.OAuthAppName("Swagger UI");
                    swaggerOptions.DocExpansion(DocExpansion.List);
                });
            }
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute("default", "{controller=Welcome}/{action=Index}/{id?}");
            });
            if (!HostingEnvironment.IsDevelopment()) {
                app.UseSpa(builder => {
                    builder.Options.SourcePath = "wwwroot/admin-ui";
                });
            }
        }
    }
}
