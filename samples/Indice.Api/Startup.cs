using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityModel;
using Indice.Api.JobHandlers;
using Indice.AspNetCore.Features.Campaigns;
using Indice.AspNetCore.Swagger;
using Indice.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
            // Configure MVC
            services.AddControllers()
                    .AddCampaignsApiEndpoints(options => {
                        options.ApiPrefix = "api";
                        options.ConfigureDbContext = builder => builder.UseSqlServer(Configuration.GetConnectionString("CampaignsDb"));
                        options.DatabaseSchema = "cmp";
                        options.RequiredScope = $"backoffice:{CampaignsApi.Scope}";
                        options.UserClaimType = JwtClaimTypes.Subject;
                    })
                    .AddJsonOptions(options => {
                        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        options.JsonSerializerOptions.WriteIndented = true;
                    });
            // Configure default CORS policy
            services.AddCors(options => options.AddDefaultPolicy(builder => {
                builder.WithOrigins(Configuration.GetSection("AllowedOrigins").Get<string[]>())
                       .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                       .WithHeaders("Authorization", "Content-Type")
                       .WithExposedHeaders("Content-Disposition");
            }));
            // Configure Swagger
            services.AddSwaggerGen(options => {
                options.IndiceDefaults(Settings);
                options.AddFluentValidationSupport();
                options.AddOAuth2AuthorizationCodeFlow(Settings);
                options.AddFormFileSupport();
                options.IncludeXmlComments(Assembly.Load(CampaignsApi.AssemblyName));
                options.AddDoc(CampaignsApi.Scope, "Campaigns API", "API for managing campaigns in the backoffice tool.");
                options.AddDoc("lookups", "Lookups API", "API for various lookups.");
            });
            // Configure authentication
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var authenticationBuilder = services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options => {
                options.Audience = Settings?.Api?.ResourceName;
                options.Authority = Settings?.Authority;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters.NameClaimType = JwtClaimTypes.Name;
                options.TokenValidationParameters.RoleClaimType = JwtClaimTypes.Role;
                options.ForwardDefaultSelector = BearerSelector.ForwardReferenceToken("Introspection");
            })
            .AddOAuth2Introspection("Introspection", options => {
                options.Authority = Settings?.Authority;
                options.CacheDuration = TimeSpan.FromMinutes(5);
                options.ClientId = Settings?.Api?.ResourceName;
                options.ClientSecret = Settings?.Api?.Secrets["Introspection"];
                options.EnableCaching = true;
            });
            services.AddScopeTransformation();
            // Configure framework & custom services
            services.AddDistributedMemoryCache();
            services.AddFilesLocal(options => {
                options.Path = "uploads";
            });
            // Setup worker host for executing background tasks.
            services.AddWorkerHost(options => {
                options.JsonOptions.JsonSerializerOptions.WriteIndented = true;
                options.AddRelationalStore(builder => {
                    builder.UseSqlServer(Configuration.GetConnectionString("WorkerDb"));
                    //builder.UseNpgsql(Configuration.GetConnectionString("WorkerDb"));
                });
            })
            .AddJob<LongRunningTaskJobHandler>()
            .WithScheduleTrigger("0 0/2 * * * ?", options => {
                options.Name = "useless-task";
                options.Description = "Does nothing for some minutes.";
                options.Group = "indice";
                options.Singleton = true;
            })
            //.AddJob<LoadAvailableAlertsJobHandler>()
            //.WithScheduleTrigger<DemoCounterModel>("0 0/1 * * * ?", options => {
            //    options.Name = "load-available-alerts";
            //    options.Description = "Load alerts for the queue.";
            //    options.Group = "indice";
            //})
            .AddJob<SendSmsJobHandler>()
            .WithQueueTrigger<SmsDto>(options => {
                options.QueueName = "send-user-sms";
                options.PollingInterval = 500;
                options.InstanceCount = 3;
            })
            .AddJob<LogSendSmsJobHandler>()
            .WithQueueTrigger<LogSmsDto>(options => {
                options.QueueName = "log-send-user-sms";
                options.PollingInterval = 500;
                options.InstanceCount = 1;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment) {
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
