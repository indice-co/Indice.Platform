using Indice.Features.Agents.Server;
using Indice.Features.Agents.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>HTTP surface for ingestion: single-file Markdown upload.</summary>
internal static class IngestionApi
{
    /// <summary>Maps the <c>/api/ingest</c> endpoint group.</summary>
    public static RouteGroupBuilder MapIngestion(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<AgentsServerOptions>>().Value;

        var allowedScopes = new[] { options.IngestRequiredScope }.FilterOutNulls().ToArray();

        var group = routes.MapGroup($"{options.PathPrefix.Value?.TrimEnd('/')}/ingest")
                          .WithName(options.GroupName)
                          .WithTags("Ingestion");
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));
        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/faq", IngestionHandlers.UploadFaq).DisableAntiforgery().AllowAnonymous()
             .WithName(nameof(IngestionHandlers.UploadFaq))
             .WithSummary("Upload a single FAQ-format Markdown file for ingestion.")
             .WithDescription("Accepts multipart/form-data with a `file` part containing FAQ-shaped Markdown (`# Category` separates segments, `## Question` introduces each question, the body until the next `##`/`#`/EOF is the answer). Each Q&A pair becomes one retrieval chunk; HeadingPath surfaces as 'Category > Question'. The file's first `#` is used as the document category — the form-data `category` is used only when the file has none.");

        return group;
    }
}
