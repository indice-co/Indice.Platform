using System;
using System.Reflection;
using Elsa;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Events;
using Indice.Features.Cases.Handlers;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Mvc.Conventions;
using Indice.Features.Cases.Services;
using Indice.Features.Cases.Services.CaseMessageService;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval;
using Indice.Features.Cases.Workflows.Interfaces;
using Indice.Features.Cases.Workflows.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Indice.Features.Cases
{
    public static class CasesApiFeatureExtensions
    {
        public static IMvcBuilder AddCasesApiEndpoints(this IMvcBuilder mvcBuilder, Action<CasesApiOptions>? configureAction = null) {
            // Add
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new CasesApiFeatureProvider()));
            var services = mvcBuilder.Services;

            // Build service provider and get IConfiguration instance.
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // Try add general settings.
            services.AddGeneralSettings(configuration);

            // Configure options given by the consumer.
            var casesApiOptions = new CasesApiOptions();
            configureAction?.Invoke(casesApiOptions);
            services.Configure<CasesApiOptions>(options => {
                options.ApiPrefix = casesApiOptions.ApiPrefix;
                options.ConfigureDbContext = casesApiOptions.ConfigureDbContext;
                options.DatabaseSchema = casesApiOptions.DatabaseSchema;
                options.ExpectedScope = casesApiOptions.ExpectedScope;
                options.UserClaimType = casesApiOptions.UserClaimType;
                options.GroupIdClaimType = casesApiOptions.GroupIdClaimType;
            }).AddSingleton(casesApiOptions);

            // Post configure MVC options.
            services.PostConfigure<MvcOptions>(options => {
                //options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeNames.Application.Json);
                //options.FormatterMappings.SetMediaTypeMappingForFormat("xlsx", FileExtensions.GetMimeType("xlsx"));
                //options.OutputFormatters.Add(new XlsxCampaignStatisticsOutputFormatter());
                options.Conventions.Add(new ApiPrefixControllerModelConvention(casesApiOptions));
            });

            // Register framework services.
            services.AddHttpContextAccessor();
            //services.AddResponseCaching();

            // Register custom services.
            services.AddTransient<IMyCaseService, MyCaseService>();
            services.AddTransient<ICaseTypeService, DbCaseTypeService>();
            services.AddTransient<ISchemaValidator, SchemaValidator>();
            services.AddTransient<ICheckpointTypeService, DbCheckpointTypeService>();
            services.AddTransient<ICaseTemplateService, CaseTemplateService>();
            services.AddTransient<IMyCaseMessageService, MyCaseMessageService>();
            services.AddTransient<IJsonTranslationService, JsonTranslationService>();

            // Register events.
            services.AddTransient<ICaseEventService, CaseEventService>();

            // Register internal handlers
            services.AddCaseEventHandler<CaseSubmittedEvent, StartWorkflowHandler>();

            // Register validators.
            // mvcBuilder.AddFluentValidation(options => options.RegisterValidatorsFromAssemblyContaining<CampaignsController>());

            // Register application DbContext.
            if (casesApiOptions.ConfigureDbContext != null) {
                services.AddDbContext<CasesDbContext>(casesApiOptions.ConfigureDbContext);
            } else {
                services.AddDbContext<CasesDbContext>(builder => builder.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            }

            // Configure authorization.
            //services.AddAuthorizationCore(authOptions => {
            //    authOptions.AddPolicy(CasesApiConstants.Policies.BeCasesManager, policy => {
            //        policy.AddAuthenticationSchemes(CasesApiConstants.AuthenticationScheme)
            //            .RequireAuthenticatedUser()
            //            .RequireAssertion(x => x.User.HasScopeClaim(casesApiOptions.ExpectedScope ?? CasesApiConstants.Scope) && x.User.CanManageCampaigns());
            //    });
            //});

            return mvcBuilder;
        }

        public static IMvcBuilder AddAdminCasesApiEndpoints(this IMvcBuilder mvcBuilder, Action<CasesApiOptions>? configureAction = null) {
            // Add
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new AdminCasesApiFeatureProvider()));
            var services = mvcBuilder.Services;

            // Build service provider and get IConfiguration instance.
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // Try add general settings.
            services.AddGeneralSettings(configuration);

            // Configure options given by the consumer.
            var casesApiOptions = new CasesApiOptions();
            configureAction?.Invoke(casesApiOptions);
            services.Configure<CasesApiOptions>(options => {
                options.ApiPrefix = casesApiOptions.ApiPrefix;
                options.ConfigureDbContext = casesApiOptions.ConfigureDbContext;
                options.DatabaseSchema = casesApiOptions.DatabaseSchema;
                options.ExpectedScope = casesApiOptions.ExpectedScope;
                options.UserClaimType = casesApiOptions.UserClaimType;
                options.GroupIdClaimType = casesApiOptions.GroupIdClaimType;
            }).AddSingleton(casesApiOptions);

            // Post configure MVC options.
            services.PostConfigure<MvcOptions>(options => {
                options.Conventions.Add(new ApiPrefixControllerModelConvention(casesApiOptions));
            });

            // Register framework services.
            services.AddHttpContextAccessor();

            // Register custom services.
            services.AddTransient<IAdminCaseService, DbAdminCaseService>();
            services.AddTransient<ICaseAuthorizationService, DbRoleCaseTypeService>();
            services.AddTransient<ICaseWorkflowService, CaseWorkflowService>();
            services.AddTransient<ICaseActionsService, CaseActionsService>();
            services.AddTransient<IAdminCaseMessageService, AdminCaseMessageService>();
            services.AddTransient<ISchemaValidator, SchemaValidator>();
            services.AddTransient<ICaseApprovalService, CaseApprovalService>();
            services.AddSmsServiceYubotoOmni(configuration)
                .AddViberServiceYubotoOmni(configuration)
                .AddEmailServiceSparkpost(configuration)
                .WithMvcRazorRendering();
            services.AddTransient<CasesMessageDescriber>();
            services.AddTransient<IJsonTranslationService, JsonTranslationService>();

            //add the provider that filters through all available ICaseAuthorizationServices
            services.AddTransient<ICaseAuthorizationProvider, AggregateCaseAuthorizationProvider>();

            // Register events.
            services.AddTransient<ICaseEventService, CaseEventService>();

            // Register internal handlers
            services.AddCaseEventHandler<CaseSubmittedEvent, StartWorkflowHandler>();

            // Register application DbContext.
            if (casesApiOptions.ConfigureDbContext != null) {
                services.AddDbContext<CasesDbContext>(casesApiOptions.ConfigureDbContext);
            } else {
                services.AddDbContext<CasesDbContext>(builder => builder.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            }

            return mvcBuilder;
        }

        /// <summary>
        /// Adding a Case Authorization Service serve as an
        /// extra way to filter the list of cases a BO user
        /// can see or have access to.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mvcBuilder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddCaseAuthorizationService<T>(this IMvcBuilder mvcBuilder) where T : ICaseAuthorizationService {
            mvcBuilder.Services.AddTransient(typeof(ICaseAuthorizationService), typeof(T));
            return mvcBuilder;
        }

        /// <summary>
        /// Registers an implementation of <see cref="ICaseEventHandler{TEvent}"/> for the specified event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to handler.</typeparam>
        /// <typeparam name="TEventHandler">The handler to user for the specified event.</typeparam>
        /// <param name="services">The services available in the application.</param>
        public static IServiceCollection AddCaseEventHandler<TEvent, TEventHandler>(this IServiceCollection services)
            where TEvent : ICaseEvent
            where TEventHandler : class, ICaseEventHandler<TEvent> {
            var serviceDescriptor = new ServiceDescriptor(typeof(ICaseEventHandler<TEvent>), typeof(TEventHandler), ServiceLifetime.Transient);
            services.TryAddEnumerable(serviceDescriptor);
            return services;
        }

        public static void AddWorkflow(this IServiceCollection services, IConfiguration configuration, Assembly? workflowAssembly) {
            services.AddElsa(elsa => {
                elsa.UseEntityFrameworkPersistence(ef => ef.UseSqlServer(configuration.GetConnectionString("WorkflowDb")), false)
                    .AddQuartzTemporalActivities()
                    .AddHttpActivities(configuration.GetSection("Elsa").GetSection("Server").Bind)
                    .AddActivitiesFrom(typeof(BaseCaseActivity).Assembly);

                // Register consumer assembly
                if (workflowAssembly != null) {
                    elsa.AddWorkflowsFrom(workflowAssembly);
                    elsa.AddActivitiesFrom(workflowAssembly);
                }
            });

            // Elsa API endpoints.
            services.AddElsaApiEndpoints();

            // For Dashboard.
            services.AddRazorPages();

            // Register Indices' bookmarks
            services.AddBookmarkProvidersFrom(typeof(AwaitApprovalBookmark).Assembly);

            // Register bookmarks from consumer assembly
            if (workflowAssembly != null) {
                services.AddBookmarkProvidersFrom(workflowAssembly);
            }

            // Register Custom Services
            services.AddScoped<IAwaitApprovalInvoker, AwaitApprovalInvoker>();
            services.AddScoped<IAwaitEditInvoker, AwaitEditInvoker>();
            services.AddScoped<IAwaitAssignmentInvoker, AwaitAssignmentInvoker>();
        }

        public static void UseWorkflow(this IApplicationBuilder app) {
            app.UseHttpActivities();
        }

        /// <summary>
        /// Override cases resources from <see cref="CasesMessageDescriber"/>.
        /// </summary>
        /// <typeparam name="TDescriber">The type of cases message describer.</typeparam>       
        /// <param name="services"></param>
        /// <returns></returns>        
        public static IServiceCollection AddCasesMessageDescriber<TDescriber>(this IServiceCollection services) where TDescriber : CasesMessageDescriber {
            services.AddScoped<CasesMessageDescriber, TDescriber>();
            return services;
        }
    }
}
