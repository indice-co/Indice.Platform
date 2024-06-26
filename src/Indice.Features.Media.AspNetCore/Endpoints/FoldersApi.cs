using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Types;

namespace Indice.Features.Media.AspNetCore.Endpoints;

/// <summary>Contains operations that expose functionality of the folder management Api.</summary>
public static class FoldersApi
{
    /// <summary>Registers the endpoints for the folder management Api.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance.</returns>
    internal static IEndpointRouteBuilder MapFolders(this IEndpointRouteBuilder builder) {
        var options = builder.ServiceProvider.GetService<IOptions<MediaApiOptions>>()?.Value ?? new MediaApiOptions();
        var group = builder.MapGroup($"{options.ApiPrefix}/media/folders")
                           .WithGroupName("media")
                           .WithTags("Folders")
                           .ProducesProblem(StatusCodes.Status401Unauthorized)
                           .ProducesProblem(StatusCodes.Status403Forbidden)
                           .ProducesProblem(StatusCodes.Status500InternalServerError)
                           .RequireAuthorization(MediaLibraryApi.Policies.BeMediaLibraryManager)
                           .WithHandledException<BusinessException>();

        var requiredScopes = options.ApiScope.Split(' ').Where(scope => !string.IsNullOrWhiteSpace(scope)).ToArray();
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", requiredScopes);

        group.MapGet("structure", FoldersHandlers.GetFolderStructure)
             .WithName(nameof(FoldersHandlers.GetFolderStructure))
             .WithSummary("Retrieves tree structure of folders.");

        group.MapGet("content", FoldersHandlers.GetFolderContent)
             .WithName(nameof(FoldersHandlers.GetFolderContent))
             .WithSummary("Retrieves the content of an existing folder.");

        group.MapGet("", FoldersHandlers.ListFolders)
             .WithName(nameof(FoldersHandlers.ListFolders))
             .WithSummary("Retrieves a list of all existing folders.");

        group.MapGet("{folderId}", FoldersHandlers.GetFolderById)
             .WithName(nameof(FoldersHandlers.GetFolderById))
             .WithSummary("Retrieves info of an existing folder.");

        group.MapPost("", FoldersHandlers.CreateFolder)
             .WithName(nameof(FoldersHandlers.CreateFolder))
             .WithSummary("Creates a new folder in the system.")
             .WithParameterValidation<CreateFolderRequest>();

        group.MapPut("{folderId}", FoldersHandlers.UpdateFolder)
             .WithName(nameof(FoldersHandlers.UpdateFolder))
             .WithSummary("Updates an existing folder in the system.")
             .WithParameterValidation<UpdateFolderRequest>();

        group.MapDelete("{folderId}", FoldersHandlers.DeleteFolder)
             .WithName(nameof(FoldersHandlers.DeleteFolder))
             .WithSummary("Deletes an existing folder.");

        return builder;
    }
}
