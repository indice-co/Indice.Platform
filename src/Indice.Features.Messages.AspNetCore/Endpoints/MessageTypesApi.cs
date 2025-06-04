using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Indice.Features.Messages.Core;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Routing;
using Indice.Features.Messages.AspNetCore.Endpoints;
using Indice.Features.Messages.Core.Models.Requests;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing message type-related operations, including retrieving, creating, updating, and deleting message types.
/// </summary>
internal static class MessageTypesApi
{
    /// <summary>Registers the endpoints for MessageTypes API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapMessageTypes(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageManagementOptions>>().Value;
        var group = routes.MapGroup(options.PathPrefix.TrimEnd('/') + "/message-types");
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("MessageTypes");
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

        group.MapGet(string.Empty, MessageTypesHandlers.GetMessageTypes)
             .WithName(nameof(MessageTypesHandlers.GetMessageTypes))
             .WithSummary("Gets the list of available message types.")
             .WithDescription(MessageTypesHandlers.GET_MESSAGE_TYPES_DESCRIPTION);

        group.MapGet("{typeId}", MessageTypesHandlers.GetMessageTypeById)
             .WithName(nameof(MessageTypesHandlers.GetMessageTypeById))
             .WithSummary("Gets a message type by its unique ID.")
             .WithDescription(MessageTypesHandlers.GET_MESSAGE_TYPE_BY_ID_DESCRIPTION);

        group.MapPost(string.Empty, MessageTypesHandlers.CreateMessageType)
             .WithName(nameof(MessageTypesHandlers.CreateMessageType))
             .WithSummary("Creates a new message type.")
             .WithDescription(MessageTypesHandlers.CREATE_MESSAGE_TYPE_DESCRIPTION)
             .WithParameterValidation<CreateMessageTypeRequest>();

        group.MapPut("{typeId}", MessageTypesHandlers.UpdateMessageType)
             .WithName(nameof(MessageTypesHandlers.UpdateMessageType))
             .WithSummary("Updates an existing message type.")
             .WithDescription(MessageTypesHandlers.UPDATE_MESSAGE_TYPE_DESCRIPTION)
             .WithParameterValidation<UpdateMessageTypeRequest>();

        group.MapDelete("{typeId}", MessageTypesHandlers.DeleteMessageType)
             .WithName(nameof(MessageTypesHandlers.DeleteMessageType))
             .WithSummary("Permanently deletes a message type.")
             .WithDescription(MessageTypesHandlers.DELETE_MESSAGE_TYPE_DESCRIPTION);

        return group;
    }
}
