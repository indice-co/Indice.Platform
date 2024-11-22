using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Indice.Types;

namespace Indice.Features.Media.AspNetCore.Endpoints;

/// <summary>Contains operations that expose functionality of the folder management Api.</summary>
public static class MediaSettingsApi
{
    /// <summary>Registers the endpoints for the settings management Api.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance.</returns>
    internal static IEndpointRouteBuilder MapMediaSettings(this IEndpointRouteBuilder builder) {
        var options = builder.ServiceProvider.GetService<IOptions<MediaApiOptions>>()?.Value ?? new MediaApiOptions();
        var group = builder.MapGroup($"{options.PathPrefix}/media/settings")
                           .WithGroupName("media")
                           .WithTags("Settings")
                           .ProducesProblem(StatusCodes.Status401Unauthorized)
                           .ProducesProblem(StatusCodes.Status403Forbidden)
                           .ProducesProblem(StatusCodes.Status500InternalServerError)
                           .RequireAuthorization(MediaLibraryApi.Policies.BeMediaLibraryManager)
                           .WithHandledException<BusinessException>();

        var requiredScopes = options.Scope.Split(' ').Where(scope => !string.IsNullOrWhiteSpace(scope)).ToArray();
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", requiredScopes);

        group.MapGet("", MediaSettingsHandlers.ListMediaSettings)
             .WithName(nameof(MediaSettingsHandlers.ListMediaSettings))
             .WithSummary("Retrieves a list of Media Settings.");

        group.MapGet("{key}", MediaSettingsHandlers.GetMediaSetting)
             .WithName(nameof(MediaSettingsHandlers.GetMediaSetting))
             .WithSummary("Retrieves an existing setting.")
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("{key}", MediaSettingsHandlers.UpdateMediaSetting)
             .WithName(nameof(MediaSettingsHandlers.UpdateMediaSetting))
             .WithSummary("Updates an existing setting's value in the system.")
             .ProducesProblem(StatusCodes.Status400BadRequest);

        return builder;
    }
}
