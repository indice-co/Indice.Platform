using System.Security.Claims;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

    public static async Task<Results<ContentHttpResult, NotFound, RedirectHttpResult>> GetActualSourceFavicon(Guid sourceId, ClaimsPrincipal currentUser, IDocumentsService documentsService, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) {
        var document = await documentsService.FindBySourceAsync(sourceId.ToString(), includeData: false, cancellationToken: cancellationToken);
        
        if (document is null) {
            return TypedResults.NotFound();
        }

        if (document.Source.StartsWith("local://", StringComparison.OrdinalIgnoreCase)) {
            return TypedResults.Redirect("/favicon.ico"); // Favicon retrieval is not supported for local sources
        }
        var httpClient = httpClientFactory.CreateClient("favicon");
        var faviconUrl = await httpClient.GetFaviconUrlAsync(document.Source, cancellationToken: cancellationToken);
        if (string.IsNullOrEmpty(faviconUrl)) {
            return TypedResults.Content(content: GlobeSvg, contentType: "image/svg+xml", contentEncoding: System.Text.Encoding.UTF8);
        }
        
        // Implementation for retrieving the favicon of the actual source document
        return TypedResults.Redirect(faviconUrl, permanent: true);
    }

    public static async Task<Results<ContentHttpResult, RedirectHttpResult>> GetFaviconFor([FromQuery] string? domain, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(domain) || 
            domain.Contains("localhost", StringComparison.OrdinalIgnoreCase)) {
            return TypedResults.Content(content: GlobeSvg, contentType: "image/svg+xml", contentEncoding: System.Text.Encoding.UTF8);
        }
        var uriBuilder = new UriBuilder(domain) {
            Scheme = "https", // Ensure HTTPS scheme
            Port = 443
        };
        var httpClient = httpClientFactory.CreateClient("favicon");
        var faviconUrl = await httpClient.GetFaviconUrlAsync(uriBuilder.ToString(), cancellationToken: cancellationToken);
        if (faviconUrl == null) {
            return TypedResults.Content(content: GlobeSvg, contentType: "image/svg+xml", contentEncoding: System.Text.Encoding.UTF8);
        }
        // Implementation for retrieving the favicon of the actual source document
        return TypedResults.Redirect(faviconUrl, permanent: true);
    }

    public const string GlobeSvg = """
        <svg xmlns="http://www.w3.org/2000/svg"
             width="64"
             height="64"
             viewBox="0 0 24 24"
             fill="none"
             stroke="#9AA0A6"
             stroke-width="1.75"
             stroke-linecap="round"
             stroke-linejoin="round">
          <circle cx="12" cy="12" r="9"/>
          <path d="M3 12h18" />
          <path d="M12 3a14 14 0 0 1 0 18"/>
          <path d="M12 3a14 14 0 0 0 0 18"/>
          <path d="M5.6 7.5c2 .8 4.2 1.2 6.4 1.2s4.4-.4 6.4-1.2"/>
          <path d="M5.6 16.5c2-.8 4.2-1.2 6.4-1.2s4.4.4 6.4 1.2"/>
        </svg>        
        """;
}
