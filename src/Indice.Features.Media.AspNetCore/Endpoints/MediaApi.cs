using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Types;
using Indice.AspNetCore.Http.Filters;

namespace Indice.Features.Media.AspNetCore.Endpoints;

/// <summary>Contains operations that expose functionality of the folder management Api.</summary>
public static class MediaApi
{
    /// <summary>Registers the endpoints for the folder management Api.</summary>
    /// <param name="builder">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/> instance.</returns>
    internal static IEndpointRouteBuilder MapMedia(this IEndpointRouteBuilder builder) {
        var options = builder.ServiceProvider.GetService<IOptions<MediaApiOptions>>()?.Value ?? new MediaApiOptions();
        var group = builder.MapGroup($"{options.ApiPrefix}")
                           .WithGroupName("media")
                           .WithTags("Media")
                           .ProducesProblem(StatusCodes.Status401Unauthorized)
                           .ProducesProblem(StatusCodes.Status403Forbidden)
                           .ProducesProblem(StatusCodes.Status500InternalServerError)
                           .RequireAuthorization(MediaLibraryApi.Policies.BeMediaLibraryManager)
                           .WithHandledException<BusinessException>();

        var requiredScopes = options.ApiScope.Split(' ').Where(scope => !string.IsNullOrWhiteSpace(scope)).ToArray();
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", requiredScopes);

        group.MapGet("/media-root/{*path}", MediaHandlers.DownloadFile)
             .WithName(nameof(MediaHandlers.DownloadFile))
             .WithSummary("Retrieves an existing file.")
             .ProducesProblem(StatusCodes.Status404NotFound)
             .Produces(StatusCodes.Status200OK, typeof(IFormFile))
             .AllowAnonymous()
             .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(30))
                                          .SetAuthorized())
             .CacheAuthorized();

        group.MapGet("/media/{fileGuid}.{format}", MediaHandlers.GetFile)
             .WithName(nameof(MediaHandlers.GetFile))
             .WithSummary("Retrieves an existing file.")
             .ProducesProblem(StatusCodes.Status404NotFound)
             .Produces(StatusCodes.Status200OK, typeof(IFormFile))
             .AllowAnonymous()
             .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(30))
                                          .SetAuthorized()
                                          .SetVaryByRouteValue(["fileGuid", "format"]))
             .CacheAuthorized();

        group.MapGet("/media/{fileId}", MediaHandlers.GetFileDetails)
             .WithName(nameof(MediaHandlers.GetFileDetails))
             .WithSummary("Retrieves details about an existing file.")
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/media/upload", MediaHandlers.UploadFile)
             .WithName(nameof(MediaHandlers.UploadFile))
             .WithSummary("Uploads a file in the system.")
             .Accepts<UploadFileRequest>("multipart/form-data")
             .LimitUpload(options.MaxFileSize, options.AcceptableFileExtensions)
             .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/media/{fileId}", MediaHandlers.UpdateFileMetadata)
             .WithName(nameof(MediaHandlers.UpdateFileMetadata))
             .WithSummary("Updates an existing file's metadata in the system.")
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .WithParameterValidation<UpdateFileMetadataRequest>();

        group.MapDelete("/media/{fileId}", MediaHandlers.DeleteFile)
             .WithName(nameof(MediaHandlers.DeleteFile))
             .WithSummary("Deletes an existing file.")
             .ProducesProblem(StatusCodes.Status400BadRequest);

        return builder;
    }
}
