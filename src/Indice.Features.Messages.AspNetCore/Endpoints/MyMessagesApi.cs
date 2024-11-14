#if NET7_0_OR_GREATER
#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Indice.Features.Messages.AspNetCore.Endpoints;
using Microsoft.Extensions.Options;
using Indice.Features.Messages.Core;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing user my messages-related operations, including retrieving messages, retrieving message types, marking messages as read, deleting messages, and retrieving attachments associated with messages.
/// </summary>
public static class MyMessagesApi
{
    /// <summary>Registers the endpoints for MyMessages API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapMyMessages(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageInboxOptions>>().Value;
        var group = routes.MapGroup(options.ApiPrefix.TrimEnd('/') + "/my/messages");
        if (!string.IsNullOrEmpty(options.GroupName)) { 
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("MyMessages");
        
        group.RequireAuthorization(pb => pb.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                                           .RequireAuthenticatedUser());

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2");

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("", MyMessagesHandlers.GetMessages)
             .WithName(nameof(MyMessagesHandlers.GetMessages))
             .WithSummary("Gets the list of all user messages using the provided ListOptions.")
             .WithDescription(MyMessagesHandlers.GET_MESSAGES_DESCRIPTION);

        group.MapGet("types", MyMessagesHandlers.GetInboxMessageTypes)
             .WithName(nameof(MyMessagesHandlers.GetInboxMessageTypes))
             .WithSummary("Gets the list of available campaign types.")
             .WithDescription(MyMessagesHandlers.GET_INBOX_MESSAGE_TYPES_DESCRIPTION);

        group.MapGet("{messageId}", MyMessagesHandlers.GetMessageById)
             .WithName(nameof(MyMessagesHandlers.GetMessageById))
             .WithSummary("Gets the message with the specified ID.")
             .WithDescription(MyMessagesHandlers.GET_MESSAGE_BY_ID_DESCRIPTION);

        group.MapPut("{messageId}/read", MyMessagesHandlers.MarkMessageAsRead)
             .WithName(nameof(MyMessagesHandlers.MarkMessageAsRead))
             .WithSummary("Marks the specified message as read.")
             .WithDescription(MyMessagesHandlers.MARK_MESSAGE_AS_READ_DESCRIPTION);

        group.MapDelete("{messageId}", MyMessagesHandlers.DeleteMessage)
             .WithName(nameof(MyMessagesHandlers.DeleteMessage))
             .WithSummary("Marks the specified message as deleted.")
             .WithDescription(MyMessagesHandlers.DELETE_MESSAGE_DESCRIPTION);

        group.MapGet("/attachments/{fileGuid}.{format}", MyMessagesHandlers.GetMessageAttachment)
             .WithName(nameof(MyMessagesHandlers.GetMessageAttachment))
             .WithSummary("Gets the attachment associated with a campaign.")
             .WithDescription(MyMessagesHandlers.GET_MESSAGE_ATTACHMENT_DESCRIPTION)
             .AllowAnonymous();

        return group;
    }

}

#nullable disable
#endif
