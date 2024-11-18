#if NET7_0_OR_GREATER
#nullable enable

using Indice.Features.Messages.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Indice.Features.Messages.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Indice.Security;
using Indice.Types;
using Indice.Features.Messages.Core.Models.Requests;

namespace Microsoft.AspNetCore.Routing;


/// <summary>
/// Provides endpoints for managing template-related operations, including retrieving, creating, updating, and deleting templates.
/// </summary>
public static class TemplatesApi
{
    /// <summary>Registers the endpoints for Templates API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapTemplates(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageManagementOptions>>().Value;
        var group = routes.MapGroup(options.ApiPrefix.TrimEnd('/') + "/templates");
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("Templates");
        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).ToArray();

        group.RequireAuthorization(pb => pb.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                                           .RequireAuthenticatedUser()
                                           .RequireCampaignsManagement()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet(string.Empty, TemplatesHandlers.GetTemplates)
             .WithName(nameof(TemplatesHandlers.GetTemplates))
             .WithSummary("Gets the list of all templates using the provided ListOptions.")
             .WithDescription(TemplatesHandlers.GET_TEMPLATES_DESCRIPTION);

        group.MapGet("{templateId}", TemplatesHandlers.GetTemplateById)
             .WithName(nameof(TemplatesHandlers.GetTemplateById))
             .WithSummary("Gets a template by its unique id.")
             .WithDescription(TemplatesHandlers.GET_TEMPLATE_BY_ID_DESCRIPTION);

        group.MapPost(string.Empty, TemplatesHandlers.CreateTemplate)
             .WithName(nameof(TemplatesHandlers.CreateTemplate))
             .WithSummary("Creates a new template in the store.")
             .WithDescription(TemplatesHandlers.CREATE_TEMPLATE_DESCRIPTION)
             .WithParameterValidation<CreateTemplateRequest>();

        group.MapPut("{templateId}", TemplatesHandlers.UpdateTemplate)
             .WithName(nameof(TemplatesHandlers.UpdateTemplate))
             .WithSummary("Updates an existing template.")
             .WithDescription(TemplatesHandlers.UPDATE_TEMPLATE_DESCRIPTION)
             .WithParameterValidation<UpdateTemplateRequest>();

        group.MapDelete("{templateId}", TemplatesHandlers.DeleteTemplate)
             .WithName(nameof(TemplatesHandlers.DeleteTemplate))
             .WithSummary("Permanently deletes a template from the store.")
             .WithDescription(TemplatesHandlers.DELETE_TEMPLATE_DESCRIPTION);

        return group;
    }
}

#nullable disable
#endif
