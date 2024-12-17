using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary> Admin attachments API. </summary>
internal static class AdminAttachmentsApi
{
    /// <summary>Downloads Admin Attachments</summary>
    public static IEndpointRouteBuilder MapAdminAttachments(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerOptions>>().Value;
        
        var group = routes.MapGroup($"{options.PathPrefix.Value!.Trim('/')}/manage/attachments");
        group.WithTags("AdminAttachments");
        
        group.WithGroupName(options.GroupName);
        
        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes("Bearer")
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
            .RequireCasesAccess(Authorization.CasesAccessLevel.Manager) // equivalent to BeCasesManager
        );

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status500InternalServerError)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("{attachmentId}/download", AdminAttachmentsHandler.DownloadAttachment)
             .WithName(nameof(AdminAttachmentsHandler.DownloadAttachment))
             .WithSummary("Download attachment in a PDF format for back-office users.");

        return routes;
    }
}