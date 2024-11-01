#if NET7_0_OR_GREATER

using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Features.Messages.Core;
using Indice.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Indice.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Indice.Features.Messages.AspNetCore.Endpoints;

namespace Microsoft.AspNetCore.Routing;

public static class MyMessagesApi
{
    public static RouteGroupBuilder MapMyMessages(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetService<IConfiguration>().GetApiSettings();
        var group = routes.MapGroup("/api/my/messages");
        group.WithTags("MyMessages");
        var allowedScopes = new[] { options.ResourceName }.Where(x => x != null).ToArray();

        group.RequireAuthorization(pb => pb.RequireAuthenticatedUser()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("", MyMessagesHandler.GetMessages)
             .WithName("GetMessages")
             .WithSummary("Gets the list of all user messages using the provided <see cref=\"ListOptions\"/>.")
             .WithDescription("<summary>Gets the list of all user messages using the provided <see cref=\"ListOptions\"/>.</summary>\r\n <param name=\"options\">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>\r\n <response code=\"200\">OK</response>")
             .Produces(StatusCodes.Status200OK, typeof(ResultSet<Message>))
             .Produces(StatusCodes.Status401Unauthorized, typeof(ProblemDetails))
             .Produces(StatusCodes.Status403Forbidden, typeof(ProblemDetails));

        group.MapGet("types", MyMessagesHandler.GetInboxMessageTypes)
                    .WithName("GetInboxMessageTypes")
                    .WithSummary("Gets the list of available campaign types.")
                    .Produces<ResultSet<MessageType>>(StatusCodes.Status200OK);

        group.MapGet("{messageId:guid}", MyMessagesHandler.GetMessageById)
             .WithName("GetMessageById")
             .WithSummary("Gets the message with the specified ID.")
             .Produces<Message>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("{messageId:guid}/read", MyMessagesHandler.MarkMessageAsRead)
             .WithName("MarkMessageAsRead")
             .WithSummary("Marks the specified message as read.")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("{messageId:guid}", MyMessagesHandler.DeleteMessage)
             .WithName("DeleteMessage")
             .WithSummary("Marks the specified message as deleted.")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status400BadRequest);

        //group.MapGet("/attachments/{fileGuid}.{format}", MyMessagesHandler.GetMessageAttachment)
        //     .WithName("GetMessageAttachment")
        //     .WithSummary("Gets the attachment associated with a campaign.")
        //     .AllowAnonymous()
        //     .Produces<IFormFile>(StatusCodes.Status200OK)
        //     .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

}

#endif
