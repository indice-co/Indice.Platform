using Asp.Versioning;
using Elsa;
using Elsa.Activities.Http.Services;
using Elsa.Activities.UserTask.Extensions;
using Elsa.Persistence.EntityFramework.Core;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Retention.Extensions;
using Elsa.Serialization;
using Elsa.Server.Api.Extensions;
using Elsa.Server.Api.Mapping;
using Elsa.Server.Api.Services;
using Duende.IdentityModel;
using Indice.Features.Cases.Workflows;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Data;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Features.Cases.Workflows.Integrations;
using Indice.Features.Cases.Workflows.Localization;
using Indice.Features.Cases.Workflows.Serialization;
using Indice.Features.Cases.Workflows.Services;
using Indice.Features.Cases.Workflows.Services.Abstractions;
using Indice.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NodaTime;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configureing the CasesWorkflow Feature.
/// </summary>
public static class CasesWorkflowFeatureExtensions
{
    /// <summary>Add case management workflow configuration.</summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> to configure.</param>
    /// <param name="configureAction">The optional configuration action.</param>
    public static IHostApplicationBuilder AddCasesWorkflow(this IHostApplicationBuilder builder, Action<CasesWorkflowOptions>? configureAction = null) {

        // Configure options given by the consumer.
        var workflowOptions = new CasesWorkflowOptions(builder.Services);
        configureAction?.Invoke(workflowOptions);
        builder.Services.Configure<CasesWorkflowOptions>(options => {
            options.ConfigureDbContext = workflowOptions.ConfigureDbContext;
            options.ConfigureRetentionServices = workflowOptions.ConfigureRetentionServices;
            options.ConfigureSmtp = workflowOptions.ConfigureSmtp;
            options.GetWorkflowAssembly = workflowOptions.GetWorkflowAssembly;
            options.RetentionServicesEnabled = workflowOptions.RetentionServicesEnabled;
            options.RetentionSpecificationFilter = workflowOptions.RetentionSpecificationFilter;
            options.ServerBasePath = workflowOptions.ServerBasePath;
            options.ServerBaseUrl = workflowOptions.ServerBaseUrl;
            options.RegisterControllers = workflowOptions.RegisterControllers;
            options.RegisterStaticFiles = workflowOptions.RegisterStaticFiles;
            options.RegisterAuthentication = workflowOptions.RegisterAuthentication;
        });
        builder.AddWorkflowInternal(workflowOptions);
        return builder;
    }



    /// <summary>Add workflow services to the case management.</summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <param name="casesWorkflowOptions">The configuration options.</param>
    internal static IHostApplicationBuilder AddWorkflowInternal(
        this IHostApplicationBuilder builder,
        CasesWorkflowOptions casesWorkflowOptions) {
        // db initializer
        var configureDatabase = casesWorkflowOptions.ConfigureDbContext ?? new Action<IServiceProvider, DbContextOptionsBuilder>((sp, ef) => ef.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("WorkflowDb")));
        
        builder.Services.AddHostedService<CasesWorkflowDbInitializerHostedService>();
        builder.Services.AddDbContextFactory<ElsaContext>(configureDatabase);

        builder.Services.AddElsa(elsa => {
            elsa.UseEntityFrameworkPersistence(configureDatabase, autoRunMigrations: false)
            .AddQuartzTemporalActivities()
            .AddHttpActivities(http => {
                http.HttpEndpointAuthorizationHandlerFactory = ActivatorUtilities.GetServiceOrCreateInstance<AuthenticationBasedHttpEndpointAuthorizationHandler>;
                if (casesWorkflowOptions.ServerBaseUrl is { } baseUrl) {
                    http.BaseUrl = new Uri(baseUrl);
                }
                if (casesWorkflowOptions.ServerBasePath is { } basePath) {
                    http.BasePath = basePath;
                }
            })
            .AddEmailActivities(casesWorkflowOptions.ConfigureSmtp)
            .AddUserTaskActivities()
            #if NET8_0
            .AddElsaStores()
            #endif
            .AddActivitiesFrom(typeof(CasesWorkflowOptions).Assembly);

            // Register consumer assembly
            var workflowAssembly = casesWorkflowOptions.GetWorkflowAssembly?.Invoke();
            if (workflowAssembly != null) {
                elsa.AddWorkflowsFrom(workflowAssembly);
                elsa.AddActivitiesFrom(workflowAssembly);
            }
        });

        if (casesWorkflowOptions.RetentionServicesEnabled) {
            builder.Services.AddRetentionServices(options => {
                options.BatchSize = 100;
                options.TimeToLive = Duration.FromDays(30);
                options.SweepInterval = Duration.FromDays(4);
                if (casesWorkflowOptions.RetentionSpecificationFilter is not null) {
                    options.ConfigureSpecificationFilter(casesWorkflowOptions.RetentionSpecificationFilter);
                }
                casesWorkflowOptions.ConfigureRetentionServices?.Invoke(options);
            });
        }

        // Elsa API endpoints. - Fixes Swagger UI when commented - commented while using minimal APIs
        builder.Services.AddElsaApiEndpointsInternal(); //this breaks the swagger UI

        // Register Indices' bookmarks
        builder.Services.AddBookmarkProvidersFrom(typeof(AwaitApprovalBookmark).Assembly);

        var workflowAssembly = casesWorkflowOptions.GetWorkflowAssembly?.Invoke();
        // Register bookmarks from consumer assembly
        if (workflowAssembly != null) {
            builder.Services.AddBookmarkProvidersFrom(workflowAssembly);
        }

        // Register Custom Services
        builder.Services.TryAddTransient<WorkflowSharedResourceService>(); // Add the service even if there is no resx file, so the runtime will not throw exception
        // Workflow integration
        builder.Services.TryAddScoped<IAwaitApprovalInvoker, AwaitApprovalInvoker>();
        builder.Services.TryAddScoped<IAwaitEditInvoker, AwaitEditInvoker>();
        builder.Services.TryAddScoped<IAwaitAssignmentInvoker, AwaitAssignmentInvoker>();
        builder.Services.TryAddScoped<IAwaitActionInvoker, AwaitActionInvoker>();
        builder.Services.AddClientCredentialsTokenManagement().AddClient("workflow", options => {
            options.TokenEndpoint = builder.Configuration.GetAuthority(tryInternal: true) + "/connect/token";
            options.ClientId = builder.Configuration.GetApiSecret("ClientId");
            options.ClientSecret = builder.Configuration.GetApiSecret("ClientSecret");
            options.Scope = builder.Configuration.GetApiResourceName();
        });
        builder.Services.AddHttpClient<CasesManagerHttpClient>((serviceProvider, httpClient) => {
                var loopbackUri = builder.Configuration.TryGetEndpoint("ServerLoopbackUri");
                httpClient.BaseAddress = string.IsNullOrWhiteSpace(loopbackUri) ?
                                            serviceProvider.GetServerLoopbackUri() :
                                            new(loopbackUri);
            })
            .AddClientCredentialsTokenHandler("workflow")
            .ClearResilienceHandlers();
        builder.Services.AddScoped<ICasesManager, CasesManagerHttp>();
        
        // Add authentication / authorization
        if (casesWorkflowOptions.RegisterAuthentication) {
            builder.Services.AddWorkflowAuthentication(builder.Configuration, casesWorkflowOptions);
            builder.Services.AddCasesWorkflowAuthoriationPolicy();
        }
        return builder;
    }

    internal const string WorkflowPolicy = nameof(WorkflowPolicy);
    /// <summary>
    /// Adds a default security policy for Elsa Controllers and Razor Pages.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configurePolicy">Override the default policy</param>
    /// <returns>The service collection for further configuration</returns>
    /// <remarks>Should be used in conjunction with the <strong>AddAuthentication().AddOpenIdConnect()</strong>
    /// because it makes use of the <strong>OpenIdConnect</strong> scheme in order to authorize a visiting user</remarks>
    public static IServiceCollection AddCasesWorkflowAuthoriationPolicy(this IServiceCollection services, Action<AuthorizationPolicyBuilder>? configurePolicy = null) {
        configurePolicy ??= policy => policy
                .AddAuthenticationSchemes("Bearer", "OpenIdConnect")
                .RequireAuthenticatedUser()
                .RequireAssertion(x => x.User.IsAdmin() || x.User.IsSystemClient() || x.User.HasRoleClaim(BasicRoleNames.CasesAdministrator));

        services.AddAuthorization(authOptions => {
            authOptions.AddPolicy(WorkflowPolicy, configurePolicy);
        });

        services.PostConfigure<MvcOptions>(options => {
            options.Conventions.Add(new GroupWorkflowActionsConvention());
            options.Conventions.Add(new AddWorkflowAuthorizeFiltersConvention());
        });

        services.PostConfigure<RazorPagesOptions>(options => {
            options.Conventions.Add(new AddWorkflowAuthorizeFiltersConvention());
        });
        return services;
    }


    internal static IServiceCollection AddElsaApiEndpointsInternal(this IServiceCollection services) {
        //Don't set Newtonsoft globally
        services.AddControllers(options => {
            //Use this conventions to set ElsaNewtonsoftJsonConvention to all controllers in Elsa.Server.Api
            options.Conventions.Add(new ElsaNewtonsoftJsonConvention());
            options.Conventions.Add(new GroupWorkflowActionsConvention());
        });
        services.AddRouting(options => { options.LowercaseUrls = true; });

        services.PostConfigure<MvcNewtonsoftJsonOptions>(options => {
            options.SerializerSettings.Converters.Add(new JsonNodeToJsonObjectAdapterConverter());
            options.SerializerSettings.Converters.Add(new JsonElementToJsonObjectAdapterConverter());
        });
        services.AddTransient(sp => {
            return new Func<Newtonsoft.Json.JsonSerializer>(() => {
                var settings = DefaultContentSerializer.CreateDefaultJsonSerializationSettings();
                settings.Converters.Add(new JsonNodeToJsonObjectAdapterConverter());
                settings.Converters.Add(new JsonElementToJsonObjectAdapterConverter());
                return Newtonsoft.Json.JsonSerializer.Create(settings);
            });
        });

        services.AddApiVersioning(
            options => {
                options.ReportApiVersions = true;
                options.DefaultApiVersion = ApiVersion.Default;
                options.AssumeDefaultVersionWhenUnspecified = true;
            }).AddMvc(); // add MVC support for API versioning sinse elsa controllers are using MVC and not minimal APIs

        services
            .AddSingleton<ConnectionConverter>()
            .AddSingleton<ActivityBlueprintConverter>()
            .AddScoped<IWorkflowBlueprintMapper, WorkflowBlueprintMapper>()
            .AddSingleton<IEndpointContentSerializerSettingsProvider, EndpointContentSerializerSettingsProvider>()
            .AddAutoMapperProfile<AutoMapperProfile>()
            .AddSignalR();
        services.AddMvc(options => {
            //Use this conventions to set ElsaNewtonsoftJsonConvention to all controllers in Elsa.Server.Api
            options.Conventions.Add(new ElsaNewtonsoftJsonConvention());
            options.Conventions.Add(new GroupWorkflowActionsConvention());
        });
        return services;
    }

    /// <summary>
    /// Add Authentication via OpenIdConnect for Workflow api and dashboard.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <param name="workflowOptions">Workflow configuration options</param>
    /// <returns>The service collection for further configuration</returns>
    internal static IServiceCollection AddWorkflowAuthentication(this IServiceCollection services, IConfiguration configuration, CasesWorkflowOptions workflowOptions) {
        // Elsa dashboard login
        services.AddAuthentication()
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => {
            options.ForwardChallenge = OpenIdConnectDefaults.AuthenticationScheme;
            options.AccessDeniedPath = "/forbidden";
        })
        // Elsa dashboard login
        .AddOpenIdConnect(authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme, displayName: "Connect with Indice", options => {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = configuration.GetAuthority();
            options.ClientId = workflowOptions.WorkflowUIClientId;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.UsePkce = true;
            options.RequireHttpsMetadata = true;
            options.MapInboundClaims = false;
            options.SaveTokens = false;
            options.AccessDeniedPath = "/forbidden";
            options.CallbackPath = "/signin-oidc";
            options.TokenValidationParameters = new TokenValidationParameters {
                NameClaimType = JwtClaimTypes.Name,
                RoleClaimType = JwtClaimTypes.Role
            };
            options.ClaimActions.MapUniqueJsonKey(JwtClaimTypes.Role, JwtClaimTypes.Role);
            options.ClaimActions.MapUniqueJsonKey("admin", "admin");
            var scopes = "email openid profile role".Split(' ');
            foreach (var scope in scopes) {
                options.Scope.Add(scope);
            }
            options.Events = new OpenIdConnectEvents {
                OnTicketReceived = context => {
                    return Task.CompletedTask;
                }
            };
        });
        return services;
    }

    /// <summary>Add workflow middleware and activities to http pipeline.</summary>
    /// <param name="app"></param>
    public static IApplicationBuilder UseCasesWorkflow(this IApplicationBuilder app) {
        var options = app.ApplicationServices.GetRequiredService<IOptions<CasesWorkflowOptions>>().Value;
        app.UseHttpActivities();
        if (options.RegisterStaticFiles) {
            app.UseStaticFiles(); // this enables razor class lib assets from workflow designer
        }
        var routes = (IEndpointRouteBuilder)app;
        if (options.RegisterControllers) {
            routes.MapControllers(); // this enables controllers from Elsa.Server.Api
        }
        routes.MapCasesWorkflowDesignerPage();
        routes.MapWorkflowManager();
        return app;
    }

    internal class GroupWorkflowActionsConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller) {
            // This is for ELSA API
            if (controller.DisplayName.Contains("elsa", StringComparison.OrdinalIgnoreCase)) {
                controller.ApiExplorer.IsVisible = false;
                controller.ApiExplorer.GroupName = "workflow-designer";
            }
        }
    }

    internal class AddWorkflowAuthorizeFiltersConvention : IControllerModelConvention, IPageApplicationModelConvention
    {
        public void Apply(ControllerModel controller) {
            // This is for ELSA API
            if (controller.DisplayName.Contains("elsa", StringComparison.OrdinalIgnoreCase)) {
                controller.Filters.Add(new AuthorizeFilter(WorkflowPolicy));
            }
        }

        public void Apply(PageApplicationModel model) {
            // This is for ELSA razor pages
            if (model.HandlerType.Namespace!.Contains("elsa", StringComparison.OrdinalIgnoreCase)) {
                model.Filters.Add(new AuthorizeFilter(WorkflowPolicy)); // razor pages are only elsa
            }
        }
    }
}
