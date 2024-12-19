using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Events;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Core.Services.CaseMessageService;
using Indice.Features.Cases.Core.Services.NoOpServices;
using Indice.Features.Cases.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Cases.Core.Events.Handlers;
using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Events;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure Case Feature dependencies
/// </summary>
public static class CasesFeatureExtensions
{

    /// <summary>Add case management Api endpoints for Customer (api/my prefix).</summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="configureAction">The optional configuration action.</param>
    public static IServiceCollection AddCasesCore(this IServiceCollection services, Action<CasesOptions>? configureAction = null) {
        // Configure options given by the consumer.
        var casesOptions = new CasesOptions(services);
        configureAction?.Invoke(casesOptions);
        CasesClaimsPrincipalExtensions.Scope = casesOptions.RequiredScope;
        services.Configure<CasesOptions>(options => {
            options.ConfigureDbContext = casesOptions.ConfigureDbContext;
            options.DatabaseSchema = casesOptions.DatabaseSchema;
            options.UserClaimType = casesOptions.UserClaimType;
            options.GroupIdClaimType = casesOptions.GroupIdClaimType;
            options.ReferenceNumberEnabled = casesOptions.ReferenceNumberEnabled;
            options.RequiredScope = casesOptions.RequiredScope;
            options.ConfigureDbSeed = casesOptions.ConfigureDbSeed;
        });

        var seedOptions = new CasesDbIntialDataOptions();
        casesOptions.ConfigureDbSeed?.Invoke(seedOptions);

        services.Configure<CasesDbIntialDataOptions>(options => {
            options.CaseTypes.AddRange(seedOptions.CaseTypes);
            options.Cases.AddRange(seedOptions.Cases);
        });
        // Register no op services.
        services.AddLookupService<NoOpLookupService>(nameof(NoOpLookupService)); // needed for factory instantiation
        services.TryAddTransient<IContactProvider, NoOpContactProvider>();
        services.TryAddTransient<ICasePdfService, NoOpCasePdfService>();
        services.AddHtmlRenderingEngineNoop(); // used by the CasesTemplate service

        // Register LookupService Factory
        services.TryAddTransient<ILookupServiceFactory, DefaultLookupServiceFactory>();

        // Register custom services.

        services.TryAddTransient<ICaseTemplateService, CaseTemplateService>();
        services.TryAddTransient<ICaseTypeService, CaseTypeService>();
        services.TryAddTransient<ISchemaValidator, CasesJsonSchemaValidator>();
        services.TryAddTransient<ICheckpointTypeService, CheckpointTypeService>();
        services.TryAddTransient<ICaseTemplateService, NoOpCaseTemplateService>();


        services.TryAddTransient<CasesMessageDescriber>();
        services.TryAddTransient<IJsonTranslationService, JsonTranslationService>();
        services.TryAddTransient<CaseSharedResourceService>(); // Add the service even if there is no resx file, so the runtime will not throw exception

        // My Services
        services.TryAddTransient<IMyCaseService, MyCaseService>();
        services.TryAddTransient<IMyCaseMessageService, MyCaseMessageService>();

        // Workflow integration 
        services.TryAddTransient<ICasesWorkflowManager, DefaultCasesWorkflowManager>();

        // register services
        services.AddSmsServiceNoop();

        // Register events.
        services.AddDefaultPlatformEventService();

        // Register internal handlers
        services.AddPlatformEventHandler<CaseSubmittedEvent, StartWorkflowHandler>();

        // Register application DbContext.
        services.AddDbContext<CasesDbContext>(casesOptions.ConfigureDbContext ?? ((sp, builder) => builder.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("CasesDb"))));

        services.AddHostedService<CasesDbInitializerHostedService>();
        return services;
    }

    /// <summary>Configures case management depedencies for managing cases at the back-office level.</summary>
    /// <returns>The <see cref="IServiceCollection"/> for further configuration</returns>
    public static IServiceCollection AddCasesManagement(this IServiceCollection services, Action<CasesOptions>? configureAction = null) {
        // Configure options given by the consumer.
        services.AddCasesCore(configureAction);
        
        // Register custom services.
        services.TryAddTransient<IAdminCaseService, AdminCaseService>();
        services.TryAddTransient<IAdminReportService, AdminReportService>();
        services.TryAddTransient<IQueryService, QueryService>();
        services.TryAddTransient<ICaseAuthorizationService, MemberAuthorizationService>();
        services.TryAddTransient<ICaseActionsService, CaseActionsService>();
        services.TryAddTransient<IAdminCaseMessageService, AdminCaseMessageService>();
        services.TryAddTransient<ISchemaValidator, CasesJsonSchemaValidator>();
        services.TryAddTransient<ICaseApprovalService, CaseApprovalService>();
        services.TryAddTransient<INotificationSubscriptionService, NotificationSubscriptionService>();
        services.TryAddTransient<IAccessRuleService, AccessRuleService>();
        

        //add the provider that filters through all available ICaseAuthorizationServices
        services.TryAddTransient<ICaseAuthorizationProvider, AggregateCaseAuthorizationProvider>();


        return services;
    }

    /// <summary>
    /// Adding a Case Authorization Service serve as an
    /// extra way to filter the list of cases a BO user
    /// can see or have access to.
    /// </summary>
    /// <typeparam name="TAuthorizationService">A custom authorization service implementation</typeparam>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The <see cref="IServiceCollection"/> for further configuration</returns>
    public static IServiceCollection AddCaseAuthorizationService<TAuthorizationService>(this IServiceCollection services) where TAuthorizationService : ICaseAuthorizationService {
        services.AddTransient(typeof(ICaseAuthorizationService), typeof(TAuthorizationService));
        return services;
    }

    /// <summary>Registers an implementation of <see cref="IPlatformEventHandler{TEvent}"/> for the specified event type.</summary>
    /// <typeparam name="TEvent">The type of the event to handler.</typeparam>
    /// <typeparam name="TEventHandler">The handler to user for the specified event.</typeparam>
    /// <param name="services">The services available in the application.</param>
    public static IServiceCollection AddCaseEventHandler<TEvent, TEventHandler>(this IServiceCollection services)
        where TEvent : ICaseEvent
        where TEventHandler : class, IPlatformEventHandler<TEvent> {
        var serviceDescriptor = new ServiceDescriptor(typeof(IPlatformEventHandler<TEvent>), typeof(TEventHandler), ServiceLifetime.Transient);
        services.TryAddEnumerable(serviceDescriptor);
        return services;
    }

    /// <summary>Override cases resources from <see cref="CasesMessageDescriber"/>.</summary>
    /// <typeparam name="TDescriber">The type of cases message describer.</typeparam>       
    /// <param name="services">The service collection to configure</param>
    /// <returns>The <see cref="IServiceCollection"/> for further configuration</returns>
    public static IServiceCollection AddCasesMessageDescriber<TDescriber>(this IServiceCollection services) where TDescriber : CasesMessageDescriber {
        services.AddScoped<CasesMessageDescriber, TDescriber>();
        return services;
    }

    /// <summary>Use this to register <typeparamref name="TLookupService"/>.</summary>
    /// <typeparam name="TLookupService"></typeparam>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="key"></param>
    public static void AddLookupService<TLookupService>(this IServiceCollection services, string key)
        where TLookupService : class, ILookupService {
        services.AddKeyedTransient<ILookupService, TLookupService>(key);
    }

    /// <summary>
    /// Configure the <see cref="IContactProvider"/> to use an Identity server backed implementation.
    /// </summary>
    /// <param name="options">The options to configure</param>
    /// <param name="configureAction">The configure action</param>
    public static void UseContactProviderIdentity(this CasesOptions options, Action<ContactProviderIdentityOptions> configureAction) {
        options.Services.Configure(configureAction);
        options.Services.AddDistributedMemoryCache();
        options.Services.AddHttpClient<IContactProvider, ContactProviderIdentityServer>((sp, httpClient) => {
            var providerOptions = sp.GetRequiredService<IOptions<ContactProviderIdentityOptions>>().Value;
            httpClient.BaseAddress = providerOptions.BaseAddress;
        });
    }
}
