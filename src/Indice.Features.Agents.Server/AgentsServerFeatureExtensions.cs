using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows;
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
    /// <param name="configureOptions">A delegate to configure the feature options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAgents(this IServiceCollection services, IConfiguration configuration, Action<AgentsServerOptions>? configureOptions = null) {
        var options = new AgentsServerOptions();
        configureOptions?.Invoke(options);
        services.AddAgentsCore(configuration, options.ConfigureAgents);

        services.AddMyProfileFeature();
        services.AddChatsFeature();
        services.AddIngestionFeature();
        services.AddUsersFeature();
        return services;
    }

    /// <summary>Registers the MyProfile feature: the profile orchestration service consumed by the <c>/api/my/profile</c> endpoints.</summary>
    public static IServiceCollection AddMyProfileFeature(this IServiceCollection services) {
        services.TryAddTransient<IMyProfileService, MyProfileService>();
        return services;
    }

    public static IServiceCollection AddChatsFeature(this IServiceCollection services) {
        services.TryAddTransient<IChatsService, ChatsService>();
        services.TryAddTransient<ISessionsStore, SessionsStore>();
        return services;
    }

    public static IServiceCollection AddIngestionFeature(this IServiceCollection services) {
        services.TryAddTransient<IEmbedder, AzureOpenAIEmbedder>();
        services.TryAddTransient<IIngestionPipeline, DefaultIngestionPipeline>();
        services.TryAddTransient<IDocumentsService, DocumentsService>();
        return services;
    }

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
    /// Maps the endpoints for the Agents feature, including MyProfile, MyChats, and Ingestion.
    /// </summary>
    /// <param name="routes"></param>
    /// <returns></returns>
    public static IEndpointRouteBuilder MapAgents (this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<AgentsOptions>>().Value;
        routes.MapMyProfile();
        routes.MapMyChats();
        if (options.Ingestion.Enabled) { 
            routes.MapIngestion();
        }
        return routes;
    }


}
