using System;
using IdentityModel.AspNetCore.AccessTokenManagement;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using ResourceOwnerPasswordFlow.Configuration;
using ResourceOwnerPasswordFlow.Services;
using ResourceOwnerPasswordFlow.Settings;

namespace ResourceOwnerPasswordFlow
{
    public class Startup
    {
        public const string CookieScheme = "ROPFCookies";

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddControllersWithViews()
                    .AddRazorRuntimeCompilation();
            // Configure authentication.
            services.AddAuthentication(CookieScheme)
                    .AddCookie(CookieScheme, options => {
                        options.LoginPath = "/account/login";
                        options.AccessDeniedPath = "/account/access-denied";
                    });
            services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>, ConfigureRopfCookie>();
            // Configure access token management.
            services.AddTransient<ITokenClientConfigurationService, CustomTokenClientConfigurationService>();
            services.AddAccessTokenManagement()
                    .ConfigureBackchannelHttpClient()
                    .AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(3)
                    }));
            services.AddHttpClient<IdentityApiService>(client => {
                var authorityUrl = Configuration.GetSection(GeneralSettings.Name).GetValue<string>(nameof(GeneralSettings.Authority));
                client.BaseAddress = new Uri(authorityUrl);
            })
            .AddUserAccessTokenHandler();
            // Configure settings.
            services.Configure<ClientSettings>(Configuration.GetSection(ClientSettings.Name));
            services.Configure<GeneralSettings>(Configuration.GetSection(GeneralSettings.Name));
            // Configure HTTP clients.
            services.AddHttpClient(HttpClientNames.IdentityServer, options => {
                var authorityUrl = Configuration.GetSection(GeneralSettings.Name).GetValue<string>(nameof(GeneralSettings.Authority));
                options.BaseAddress = new Uri(authorityUrl);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment) {
            if (environment.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/home/error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
