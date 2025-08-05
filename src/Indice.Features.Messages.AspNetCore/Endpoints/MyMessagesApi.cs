using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Indice.Features.Messages.AspNetCore.Endpoints;
using Microsoft.Extensions.Options;
using Indice.Features.Messages.Core;
using Indice.Types;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing user my messages-related operations, including retrieving messages, retrieving message types, marking messages as read, deleting messages, and retrieving attachments associated with messages.
/// </summary>
internal static class MyMessagesApi
{
    /// <summary>Registers the endpoints for MyMessages API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapMyMessages(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageInboxOptions>>().Value;
        var group = routes.MapGroup(options.PathPrefix.Length > 1 ? options.PathPrefix.TrimEnd('/') : options.PathPrefix);
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("MyMessages");

        group.RequireAuthorization(pb => pb.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                                           .RequireAuthenticatedUser());

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2");

        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("my/messages", MyMessagesHandlers.GetMessages)
             .WithName(nameof(MyMessagesHandlers.GetMessages))
             .WithSummary("Gets the list of all user messages using the provided ListOptions.")
             .WithDescription(MyMessagesHandlers.GET_MESSAGES_DESCRIPTION);

        group.MapGet("my/messages/{messageId}", MyMessagesHandlers.GetMessageById)
             .WithName(nameof(MyMessagesHandlers.GetMessageById))
             .WithSummary("Gets the message with the specified ID.")
             .WithDescription(MyMessagesHandlers.GET_MESSAGE_BY_ID_DESCRIPTION);

        group.MapPut("my/messages/{messageId}/read", MyMessagesHandlers.MarkMessageAsRead)
             .WithName(nameof(MyMessagesHandlers.MarkMessageAsRead))
             .WithSummary("Marks the specified message as read.")
             .WithDescription(MyMessagesHandlers.MARK_MESSAGE_AS_READ_DESCRIPTION);

        group.MapPut("my/messages/all/read/{searchTerm}", MyMessagesHandlers.MarkAllAsRead)
             .WithName(nameof(MyMessagesHandlers.MarkAllAsRead))
             .WithSummary("Marks all user messages as read.")
             .WithDescription(MyMessagesHandlers.MARK_ALL_MESSAGE_AS_READ_DESCRIPTION);

        group.MapPut("my/messages/all/unread/{searchTerm}", MyMessagesHandlers.MarkAllAsUnRead)
             .WithName(nameof(MyMessagesHandlers.MarkAllAsUnRead))
             .WithSummary("Marks all user messages as unread.")
             .WithDescription(MyMessagesHandlers.MARK_ALL_MESSAGE_AS_UNREAD_DESCRIPTION);


        group.MapPut("my/messages/{messageId}/unread", MyMessagesHandlers.MarkMessageAsUnread)
             .WithName(nameof(MyMessagesHandlers.MarkMessageAsUnread))
             .WithSummary("Marks the specified message as read.")
             .WithDescription(MyMessagesHandlers.MARK_MESSAGE_AS_UNREAD_DESCRIPTION);

        group.MapDelete("my/messages/{messageId}", MyMessagesHandlers.DeleteMessage)
             .WithName(nameof(MyMessagesHandlers.DeleteMessage))
             .WithSummary("Marks the specified message as deleted.")
             .WithDescription(MyMessagesHandlers.DELETE_MESSAGE_DESCRIPTION);

        group.MapGet("messages/attachments/{fileGuid}.{format}", MyMessagesHandlers.GetMessageAttachment)
             .WithName(nameof(MyMessagesHandlers.GetMessageAttachment))
             .WithSummary("Gets the attachment associated with a campaign.")
             .WithDescription(MyMessagesHandlers.GET_MESSAGE_ATTACHMENT_DESCRIPTION)
             .ExcludeFromDescription()
             .AllowAnonymous();

        group.MapGet("messages/types", MyMessagesHandlers.GetInboxMessageTypes)
            .WithName(nameof(MyMessagesHandlers.GetInboxMessageTypes))
            .WithSummary("Gets the list of available campaign types.")
            .WithDescription(MyMessagesHandlers.GET_INBOX_MESSAGE_TYPES_DESCRIPTION);


        group.MapGet("my/communication-preferences", MyMessagesHandlers.GetMyCommunicationPreferences)
             .WithName(nameof(MyMessagesHandlers.GetMyCommunicationPreferences))
             .WithSummary("Gets user communication prefereces.")
             .WithDescription(MyMessagesHandlers.GET_COMMUNICATION_PREFERENCES_DESCRIPTION);

        group.MapPut("my/communication-preferences", MyMessagesHandlers.UpdateMyCommunicationPreferences)
             .WithName(nameof(MyMessagesHandlers.UpdateMyCommunicationPreferences))
             .WithSummary("Updates user's communication prefereces.")
             .WithDescription(MyMessagesHandlers.UPDATE_COMMUNICATION_PREFERENCES_DESCRIPTION);
        return group;
    }

}
