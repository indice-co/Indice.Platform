using System.Net.Mime;
using System.Text.Json.Nodes;
using Indice.Features.Agents.Server;
using Indice.Features.Agents.Server.Endpoints;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

        var group = routes.MapGroup($"{options.PathPrefix.Value?.TrimEnd('/')}/documents")
                          .WithName(options.GroupName)
                          .WithTags("Documents");
        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireAssertion(context =>  (!string.IsNullOrEmpty(options.IngestRequiredScope) && context.User.HasScope(options.IngestRequiredScope)) || context.User.IsInRole(BasicRoleNames.AgentsAdmin) || context.User.IsAdmin()));
        
        group.WithOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("ingest", IngestionHandlers.DocumentIngest).DisableAntiforgery()
             .WithName(nameof(IngestionHandlers.DocumentIngest))
             .WithSummary("Ingests a document into the knowledge base.")
             .WithDescription("Uploads a single Markdown file and processes it into the knowledge base.")
             .WithExampleRequestBody(JsonNode.Parse("""
                 {
                   "language": "el",
                   "isPrivate": false,
                   "category": "FAQ",
                   "actualSourceUrl": "",
                   "documentType": "MarkdownFaq"
                 }
                 """)!, contentType: MediaTypeNames.Multipart.FormData);

        group.MapPost("clear",IngestionHandlers.Clear)
            .WithName(nameof(IngestionHandlers.Clear))
            .WithSummary("Clears the knowledge base.")
            .WithDescription("Clears all documents and their chunks from the knowledge base.");

        return group;
    }
}
