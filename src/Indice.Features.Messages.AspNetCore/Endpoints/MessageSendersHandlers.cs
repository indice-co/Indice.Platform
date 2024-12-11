using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Indice.Features.Messages.Core.Models;
using Indice.Types;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.AspNetCore.Endpoints;
internal static class MessageSendersHandlers
{
    public static async Task<Ok<ResultSet<MessageSender>>> GetMessageSenders([AsParameters] ListOptions options, [AsParameters] MessageSenderListFilter filter, IMessageSenderService messageSenderService) {
        var messageSenders = await messageSenderService.GetList(options, filter);
        return TypedResults.Ok(messageSenders);
    }

    public static async Task<Results<Ok<MessageSender>, NotFound>> GetMessageSenderById(IMessageSenderService messageSenderService, Guid senderId) {
        var messageSender = await messageSenderService.GetById(senderId);
        if (messageSender is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(messageSender);
    }

    public static async Task<CreatedAtRoute<MessageSender>> CreateMessageSender(IMessageSenderService messageSenderService, CreateMessageSenderRequest request) {
        var messageSender = await messageSenderService.Create(request);
        return TypedResults.CreatedAtRoute(messageSender, nameof(GetMessageSenderById), new { senderId = messageSender.Id });
    }

    public static async Task<Results<NoContent, ValidationProblem>> UpdateMessageSender(IMessageSenderService messageSenderService, Guid senderId, UpdateMessageSenderRequest request) {
        await messageSenderService.Update(senderId, request);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, ValidationProblem>> DeleteMessageSender(IMessageSenderService messageSenderService, Guid senderId) {
        await messageSenderService.Delete(senderId);
        return TypedResults.NoContent();
    }

    #region Descriptions
    public static readonly string GET_MESSAGE_SENDERS_DESCRIPTION = @"
Gets the list of available message senders.

Parameters:
- options: List parameters used to navigate through collections, including sort, search, page number, and page size.
- filter: Contains filter criteria for the message senders.
";

    public static readonly string GET_MESSAGE_SENDER_BY_ID_DESCRIPTION = @"
Gets a message sender by its unique ID.

Parameters:
- senderId: The unique ID of the message sender.
";

    public static readonly string CREATE_MESSAGE_SENDER_DESCRIPTION = @"
Creates a new message sender.

Parameters:
- request: Contains the details of the message sender to be created.
";

    public static readonly string UPDATE_MESSAGE_SENDER_DESCRIPTION = @"
Updates an existing message sender.

Parameters:
- senderId: The unique ID of the message sender.
- request: Contains the updated details of the message sender.
";

    public static readonly string DELETE_MESSAGE_SENDER_DESCRIPTION = @"
Permanently deletes a message sender.

Parameters:
- senderId: The unique ID of the message sender.
";
    #endregion
}
