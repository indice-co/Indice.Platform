#if NET7_0_OR_GREATER
#nullable enable

using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class MyMessagesHandlers
{
    
    public static async Task<Ok<ResultSet<Message>>> GetMessages(
        [AsParameters] ListOptions options,
        [AsParameters] MessagesFilter filter,
        IMessageService messageService,
        IOptions<MessageInboxOptions> campaignEndpointOptions,
        ClaimsPrincipal currentUser
    ) {
        var userCode = currentUser.FindFirstValue(campaignEndpointOptions.Value.UserClaimType);
        var messages = await messageService.GetList(userCode, ListOptions.Create(options, filter));

        return TypedResults.Ok(messages); 
    }

    public static async Task<Ok<ResultSet<MessageType>>> GetInboxMessageTypes(
       [AsParameters] ListOptions options,
       IMessageTypeService messageTypeService
    ) {
        var campaignTypes = await messageTypeService.GetList(options);
        return TypedResults.Ok(campaignTypes);
    }

    public static async Task<Results<Ok<Message>, NotFound>> GetMessageById(
        IMessageService messageService,
        IOptions<MessageInboxOptions> campaignEndpointOptions,
        Guid messageId,
        MessageChannelKind? channel,
        ClaimsPrincipal currentUser
    ) {
        var userCode = currentUser.FindFirstValue(campaignEndpointOptions.Value.UserClaimType);
        var message = await messageService.GetById(messageId, userCode, channel);
        if (message is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(message);
    }


    public static async Task<NoContent> MarkMessageAsRead(
        IMessageService messageService,
        IOptions<MessageInboxOptions> campaignEndpointOptions,
        Guid messageId,
        ClaimsPrincipal currentUser
    ) {
        var userCode = currentUser.FindFirstValue(campaignEndpointOptions.Value.UserClaimType);
        await messageService.MarkAsRead(messageId, userCode);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> DeleteMessage(
        IMessageService messageService,
        IOptions<MessageInboxOptions> campaignEndpointOptions,
        Guid messageId,
        ClaimsPrincipal currentUser
    ) {
        var userCode = currentUser.FindFirstValue(campaignEndpointOptions.Value.UserClaimType);
        await messageService.MarkAsDeleted(messageId, userCode);
        return TypedResults.NoContent();
    }

    public static async Task<Results<FileContentHttpResult, NotFound>> GetMessageAttachment(
        IFileServiceFactory fileServiceFactory,
        Base64Id fileGuid,
        string format) {
        var fileResult = await CampaignsHandlers.GetFile(fileServiceFactory, "campaigns", fileGuid, format);
        return fileResult;
    }

    #region Descriptions
    public static readonly string GET_MESSAGES_DESCRIPTION = @"
    Gets the list of all user messages using the provided ListOptions.

    Parameters:
    - options: List parameters used to navigate through collections, including sort, search, page number, and page size.
    ";

    public static readonly string GET_INBOX_MESSAGE_TYPES_DESCRIPTION = @"
    Gets the list of available campaign types.

    Parameters:
    - options: List parameters used to navigate through collections, including sort, search, page number, and page size.
    ";

    public static readonly string GET_MESSAGE_BY_ID_DESCRIPTION = @"
    Gets the message with the specified ID.

    Parameters:
    - messageId: The ID of the message.
    - channel: The channel of the message.
    ";

    public static readonly string MARK_MESSAGE_AS_READ_DESCRIPTION = @"
    Marks the specified message as read.

    Parameters:
    - messageId: The ID of the message.
    ";

    public static readonly string DELETE_MESSAGE_DESCRIPTION = @"
    Marks the specified message as deleted.

    Parameters:
    - messageId: The ID of the message.
    ";

    public static readonly string GET_MESSAGE_ATTACHMENT_DESCRIPTION = @"
    Gets the attachment associated with a campaign.

    Parameters:
    - fileGuid: Contains the photo's ID.
    - format: Contains the format of the uploaded attachment extension.
    ";

    #endregion

}
#nullable disable
#endif