using System.Security.Claims;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Agents.Server.Endpoints;

internal static class SourcesHandlers
{
    public static async Task<Results<FileContentHttpResult, NotFound, UnauthorizedHttpResult>> GetActualSource(string path, bool? download, ClaimsPrincipal currentUser, IDocumentsService documentsService, CancellationToken cancellationToken) {
        var document = await documentsService.FindBySourceAsync($"local://{path}", includeData: true, cancellationToken: cancellationToken);
        
        if (document == null || document.Data == null) {
            return TypedResults.NotFound();
        }
        
        if (document.IsPrivate && (currentUser.Identity?.IsAuthenticated != true)) {
            return TypedResults.Unauthorized();
        }
        var contentType = document.ContentType.StartsWith("text", StringComparison.OrdinalIgnoreCase) ? 
                          $"{document.ContentType}; charset=utf-8" : 
                          document.ContentType;
        
        // Implementation for retrieving the actual source document
        return TypedResults.File(document.Data, contentType: contentType,
                                 fileDownloadName: download == true ? document.FileName : null,  // trigger download with content disposition if 'download' query parameter is true
                                 lastModified: document.LastModified);
    }

    public static async Task<Results<FileContentHttpResult, NotFound, RedirectHttpResult>> GetActualSourceFavicon(Guid sourceId, ClaimsPrincipal currentUser, IDocumentsService documentsService, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) {
        var document = await documentsService.FindBySourceAsync(sourceId.ToString(), includeData: false, cancellationToken: cancellationToken);
        
        if (document is null) {
            return TypedResults.NotFound();
        }

        if (document.Source.StartsWith("local://", StringComparison.OrdinalIgnoreCase)) {
            return TypedResults.Redirect("/favicon.ico"); // Favicon retrieval is not supported for local sources
        }
        var httpClient = httpClientFactory.CreateClient("favicon");
        var faviconUrl = await httpClient.GetFaviconUrlAsync(document.Source, cancellationToken: cancellationToken);
        // Implementation for retrieving the favicon of the actual source document
        return TypedResults.Redirect(faviconUrl, permanent: true);
    }
}
