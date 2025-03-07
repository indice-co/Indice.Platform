using Indice.Features.Messages.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Indice.Features.Messages.Core;
using Indice.Security;
using Indice.Types;
using Indice.Features.Messages.Core.Models.Requests;

namespace Microsoft.AspNetCore.Routing;
/// <summary>
/// Provides endpoints for managing message sender-related operations, including retrieving, creating, updating, and deleting message senders.
/// </summary>
internal static class MessageSendersApi
{
    /// <summary>Registers the endpoints for MessageSenders API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapMessageSenders(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageManagementOptions>>().Value;
        var group = routes.MapGroup(options.PathPrefix.TrimEnd('/') + "/message-senders");
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("MessageSenders");
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

        group.MapGet(string.Empty, MessageSendersHandlers.GetMessageSenders)
             .WithName(nameof(MessageSendersHandlers.GetMessageSenders))
             .WithSummary("Gets the list of available message senders.")
             .WithDescription(MessageSendersHandlers.GET_MESSAGE_SENDERS_DESCRIPTION);

        group.MapGet("{senderId}", MessageSendersHandlers.GetMessageSenderById)
             .WithName(nameof(MessageSendersHandlers.GetMessageSenderById))
             .WithSummary("Gets a message sender by its unique ID.")
             .WithDescription(MessageSendersHandlers.GET_MESSAGE_SENDER_BY_ID_DESCRIPTION);

        group.MapPost(string.Empty, MessageSendersHandlers.CreateMessageSender)
             .WithName(nameof(MessageSendersHandlers.CreateMessageSender))
             .WithSummary("Creates a new message sender.")
             .WithDescription(MessageSendersHandlers.CREATE_MESSAGE_SENDER_DESCRIPTION)
             .WithParameterValidation<CreateMessageSenderRequest>();

        group.MapPut("{senderId}", MessageSendersHandlers.UpdateMessageSender)
             .WithName(nameof(MessageSendersHandlers.UpdateMessageSender))
             .WithSummary("Updates an existing message sender.")
             .WithDescription(MessageSendersHandlers.UPDATE_MESSAGE_SENDER_DESCRIPTION)
             .WithParameterValidation<UpdateMessageSenderRequest>();

        group.MapDelete("{senderId}", MessageSendersHandlers.DeleteMessageSender)
             .WithName(nameof(MessageSendersHandlers.DeleteMessageSender))
             .WithSummary("Permanently deletes a message sender.")
             .WithDescription(MessageSendersHandlers.DELETE_MESSAGE_SENDER_DESCRIPTION);

        return group;
    }
}
