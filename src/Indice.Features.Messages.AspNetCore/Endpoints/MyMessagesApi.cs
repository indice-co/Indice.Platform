#if NET7_0_OR_GREATER

using Indice.Features.Messages.Core.Models;
using Indice.Types;
using Microsoft.AspNetCore.Mvc;
using Indice.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Indice.Features.Messages.AspNetCore.Endpoints;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing user my messages-related operations, including retrieving messages, retrieving message types, marking messages as read, deleting messages, and retrieving attachments associated with messages.
/// </summary>
public static class MyMessagesApi
{
    /// <summary>Registers the endpoints for MyMessages API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapMyMessages(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetService<IConfiguration>().GetApiSettings();
        var group = routes.MapGroup("/api/my/messages");
        group.WithTags("MyMessages");
        var allowedScopes = new[] { options.ResourceName }.Where(x => x != null).ToArray();

        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

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

        group.MapGet("{messageId:guid}", MyMessagesHandlers.GetMessageById)
             .WithName(nameof(MyMessagesHandlers.GetMessageById))
             .WithSummary("Gets the message with the specified ID.")
             .WithDescription(MyMessagesHandlers.GET_MESSAGE_BY_ID_DESCRIPTION);

        group.MapPut("{messageId:guid}/read", MyMessagesHandlers.MarkMessageAsRead)
             .WithName(nameof(MyMessagesHandlers.MarkMessageAsRead))
             .WithSummary("Marks the specified message as read.")
             .WithDescription(MyMessagesHandlers.MARK_MESSAGE_AS_READ_DESCRIPTION);

        group.MapDelete("{messageId:guid}", MyMessagesHandlers.DeleteMessage)
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

#endif
