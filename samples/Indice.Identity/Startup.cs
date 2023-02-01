using System.Globalization;
using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hellang.Middleware.ProblemDetails;
using IdentityModel;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Events;
using Indice.AspNetCore.Identity.Localization;
using Indice.AspNetCore.Middleware;
using Indice.Configuration;
using Indice.Identity.Configuration;
using Indice.Identity.Hubs;
using Indice.Identity.Security;
using Indice.Identity.Services;
using Indice.Security;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Indice.Identity
{
    /// <summary>Bootstrap class for the application.</summary>
    public class Startup
    {
        /// <summary>Creates a new instance of <see cref="Startup"/>.</summary>
        /// <param name="hostingEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public Startup(IWebHostEnvironment hostingEnvironment, IConfiguration configuration) {
            HostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Settings = Configuration.GetSection(GeneralSettings.Name).Get<GeneralSettings>();
        }

        /// <summary>Provides information about the web hosting environment an application is running in.</summary>
        public IWebHostEnvironment HostingEnvironment { get; }
        /// <summary>Represents a set of key/value application configuration properties.</summary>
        public IConfiguration Configuration { get; }
        /// <summary>General settings for an ASP.NET Core application.</summary>
        public GeneralSettings Settings { get; }
        /// <summary>Represents a type used to perform logging.</summary>
        public ILogger<Startup> Logger { get; }
        public bool HasSignalRConnection { get; private set; }

        /// <summary>This method gets called by the runtime. Use this method to add services to the container.</summary>
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
            services.AddAuthorization(options => {
                options.AddPolicy("BeDeviceAuthenticated", policy => {
                    policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(context =>
                              context.User.HasScope(IdentityServerApi.Scope) && (
                                  context.User.HasClaim(JwtClaimTypes.AuthenticationMethod, CustomGrantTypes.DeviceAuthentication) ||
                                 (context.User.IsAdmin() && !HostingEnvironment.IsProduction())
                              )
                          );
                });
            });
            services.AddIdentityConfig(Configuration);
            services.AddIdentityServerConfig(HostingEnvironment, Configuration, Settings);
            services.AddProblemDetailsConfig(HostingEnvironment);
            services.ConfigureNonBreakingSameSiteCookies();
            services.AddSmsServiceYubotoOmni(Configuration);
            services.AddEmailServiceSparkpost(Configuration)
                    .WithMvcRazorRendering();
            services.Configure<AntiforgeryOptions>(options => {
                options.HeaderName = "X-XSRF-TOKEN";
            });
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
                       .AddConnectSrc("wss://indice-identity.service.signalr.net")
                       .AddConnectSrc("https://indice-identity.service.signalr.net")
                       .AddFrameAncestors("https://localhost:2002");
            })
            .AddPlatformEventHandler<DeviceDeletedEvent, DeviceDeletedEventHandler>();
            var signalRServiceConnection = Configuration.GetConnectionString("SignalRService");
            HasSignalRConnection = !string.IsNullOrWhiteSpace(signalRServiceConnection);
            if (HasSignalRConnection) {
                services.AddSingleton<IUserIdProvider, NameUserIdProvider>();
                services.AddSignalR(options => {
                    options.EnableDetailedErrors = !HostingEnvironment.IsProduction();
                });
                //.AddAzureSignalR(options => {
                //    options.ConnectionString = signalRServiceConnection;
                //});
            }
            services.AddPlatformEventHandler<DeviceDeletedEvent, DeviceDeletedEventHandler>();
            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblyContaining<Startup>();
            services.AddFluentValidationClientsideAdapters();
            //services.AddClientIpRestrinctions();
            services.AddClientIpRestrinctions(options => {
                options.HttpStatusCode = System.Net.HttpStatusCode.NotFound;
                options.AddIpAddressList("MyWhiteList", "192.168.1.5");
                options.MapPath("/admin", "MyWhiteList");
                options.MapPath("/login", "MyWhiteList");
                options.IgnorePath("/login?ReturnUrl=", "GET");
            });
        }

        /// <summary>This method gets called by the runtime. Use this method to configure the HTTP request pipeline.</summary>
        /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
        public void Configure(IApplicationBuilder app) {
            if (HostingEnvironment.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.IdentityServerStoreSetup<ExtendedConfigurationDbContext>(Clients.Get(), Resources.GetIdentityResources(), Resources.GetApis(), Resources.GetApiScopes());
            } else {
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            app.UseClientIpRestrictions();
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
                endpoints.MapRazorPages();
                endpoints.MapDefaultControllerRoute();
                if (HasSignalRConnection) {
                    endpoints.MapHub<MultiFactorAuthenticationHub>("/mfa");
                }
            });
        }
    }
}
