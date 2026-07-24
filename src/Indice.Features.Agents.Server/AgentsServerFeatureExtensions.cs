using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Ingestion;
using Indice.Features.Agents.Server;
using Indice.Features.Agents.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods to register the Agent Server Feature on an app host</summary>
public static class AgentsServerFeatureExtensions
{

    /// <summary>
    /// Registers the Agents feature with the specified configuration options.
    /// </summary>
    /// <param name="services">The service collection to add the feature to.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureOptions">A delegate to configure the feature options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAgents(this IServiceCollection services, IConfiguration configuration, Action<AgentsServerOptions>? configureOptions = null) {
        var options = new AgentsServerOptions();
        configureOptions?.Invoke(options);
        if(configureOptions is not null) { 
            services.Configure(configureOptions);
        }
        services.AddTransient<ISourceLinkGenerator, SourceLinkGenerator>();
        services.AddAgentsCore(configuration, options.ConfigureAgents);
        services.AddMyProfileFeature();
        services.AddChatsFeature();
        services.AddIngestionFeature();
        services.AddUsersFeature();
        if (options.AllowAnonymousChatCreation) {
            services.AddHttpClient<IGuestTokenService, GuestTokenService>();
        }
        return services;
    }

    /// <summary>Registers the MyProfile feature: the profile orchestration service consumed by the <c>/api/my/profile</c> endpoints.</summary>
    public static IServiceCollection AddMyProfileFeature(this IServiceCollection services) {
        services.TryAddTransient<IMyProfileService, MyProfileService>();
        return services;
    }

    /// <summary>
    /// Registers the Chats feature.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddChatsFeature(this IServiceCollection services) {
        services.TryAddTransient<IChatsService, ChatsService>();
        services.TryAddTransient<IConversationStore, ConversationStore>();
        return services;
    }

    /// <summary>
    /// Registers the Ingestion feature, including the ingestion pipeline and documents service.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddIngestionFeature(this IServiceCollection services) {
        services.TryAddTransient<IIngestionPipeline, IngestionPipeline>();
        services.TryAddTransient<IDocumentsService, DocumentsService>();
        return services;
    }

    /// <summary>
    /// Registers the Users feature, including the user claims provider and the users service.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddUsersFeature(this IServiceCollection services) {
        services.AddHttpContextAccessor();
        services.Replace(ServiceDescriptor.Transient<WorkflowClaimsPrincipalSelector>(sp => {
            return () => {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                return httpContextAccessor.HttpContext?.User;
            };
        }));
        services.TryAddTransient<UserClaimsAIContextProvider>();
        services.TryAddTransient<IUsersService, UsersService>();
        return services;
    }

    /// <summary>
    /// Maps the endpoints for the Agents feature, including MyProfile, MyChats, Sources, and Ingestion.
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static IEndpointRouteBuilder MapAgents (this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<AgentsOptions>>().Value;
        routes.MapMyProfile();
        routes.MapMyChats();
        routes.MapSources();
        routes.MapAgents();
        if (options.Ingestion.Enabled) { 
            routes.MapIngestion();
        }
        return routes;
    }


}
