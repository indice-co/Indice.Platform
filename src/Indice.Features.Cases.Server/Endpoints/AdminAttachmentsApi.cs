using Indice.Features.Cases.Server.Options;
using Indice.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

/// <summary> Admin attachments API. </summary>
public static class AdminAttachmentsApi
{
    /// <summary>Downloads Admin Attachments</summary>
    public static IEndpointRouteBuilder MapAdminAttachments(this IEndpointRouteBuilder routes) {
        CaseServerEndpointOptions options = routes.ServiceProvider.GetRequiredService<IOptions<CaseServerEndpointOptions>>().Value;
        var group = routes.MapGroup($"{options.ApiPrefix}/manage/attachments");
        group.WithTags("AdminAttachments");
        group.WithGroupName(options.GroupName);
        var allowedScopes = new[] { options.ApiScope }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(CasesApiConstants.AuthenticationScheme)
            .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        ).RequireAuthorization(CasesApiConstants.Policies.BeCasesManager);

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