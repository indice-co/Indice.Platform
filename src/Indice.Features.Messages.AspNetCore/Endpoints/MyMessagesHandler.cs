#if NET7_0_OR_GREATER

using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class MyMessagesHandler
{
    
    public static async Task<IResult> GetMessages(
        [AsParameters] ListOptions options,
        [AsParameters] MessagesFilter filter,
        IMessageService messageService,
        IOptions<MessageInboxOptions> campaignEndpointOptions,
        HttpContext context
    ) {
        var userCode = context.User.FindFirstValue(campaignEndpointOptions.Value.UserClaimType);
        var messages = await messageService.GetList(userCode, ListOptions.Create(options, filter));

        return Results.Ok(messages); 
    }

    public static async Task<Ok<ResultSet<MessageType>>> GetInboxMessageTypes(
       IMessageTypeService messageTypeService,
       [AsParameters] ListOptions options,
       [AsParameters] MessagesFilter filter
    ) {
        var campaignTypes = await messageTypeService.GetList(ListOptions.Create(options, filter));
        return TypedResults.Ok(campaignTypes);
    }

    public static async Task<Results<Ok<Message>, NotFound>> GetMessageById(
        IMessageService messageService,
        IOptions<MessageInboxOptions> campaignEndpointOptions,
        Guid messageId,
        MessageChannelKind? channel,
        HttpContext context
    ) {
        var userCode = context.User.FindFirstValue(campaignEndpointOptions.Value.UserClaimType);
        var message = await messageService.GetById(messageId, userCode, channel);
        return message != null ? TypedResults.Ok(message) : TypedResults.NotFound();
    }


    public static async Task<NoContent> MarkMessageAsRead(
        IMessageService messageService,
        IOptions<MessageInboxOptions> campaignEndpointOptions,
        Guid messageId,
        HttpContext context
    ) {
        var userCode = context.User.FindFirstValue(campaignEndpointOptions.Value.UserClaimType);
        await messageService.MarkAsRead(messageId, userCode);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> DeleteMessage(
        IMessageService messageService,
        IOptions<MessageInboxOptions> campaignEndpointOptions,
        Guid messageId,
        HttpContext context
    ) {
        var userCode = context.User.FindFirstValue(campaignEndpointOptions.Value.UserClaimType);
        await messageService.MarkAsDeleted(messageId, userCode);
        return TypedResults.NoContent();
    }

    // uncomment when campaings controller is imported
    //public static async Task<Results<FileContentHttpResult, NotFound>> GetMessageAttachment(
    //    [FromServices] IFileServiceFactory fileServiceFactory,
    //    [FromRoute] Base64Id fileGuid,
    //    [FromRoute] string format) {
    //    var fileResult = await GetFile("campaigns", fileGuid, format);
    //    return fileResult != null ? TypedResults.File(fileResult.FileContent, fileResult.ContentType) : TypedResults.NotFound();
    //}


}

#endif