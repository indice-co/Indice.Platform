using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class MessageTypesHandlers
{
    public static async Task<Ok<ResultSet<MessageType>>> GetMessageTypes([AsParameters] ListOptions options, IMessageTypeService messageTypeService) {
        var messageTypes = await messageTypeService.GetList(options);
        return TypedResults.Ok(messageTypes);
    }

    public static async Task<Results<Ok<MessageType>, NotFound>> GetMessageTypeById(Guid typeId, IMessageTypeService messageTypeService) {
        var messageType = await messageTypeService.GetById(typeId);
        if (messageType is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(messageType);
    }

    public static async Task<CreatedAtRoute<MessageType>> CreateMessageType(CreateMessageTypeRequest request, IMessageTypeService messageTypeService) {
        var messageType = await messageTypeService.Create(request);
        return TypedResults.CreatedAtRoute(messageType, nameof(GetMessageTypeById), new { typeId = messageType.Id });
    }

    public static async Task<NoContent> UpdateMessageType(Guid typeId, UpdateMessageTypeRequest request, IMessageTypeService messageTypeService) {
        await messageTypeService.Update(typeId, request);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> DeleteMessageType(Guid typeId, IMessageTypeService messageTypeService) {
        await messageTypeService.Delete(typeId);
        return TypedResults.NoContent();
    }

    #region Descriptions

    public static readonly string GET_MESSAGE_TYPES_DESCRIPTION = @"
Retrieves a list of available message types.
    
Parameters:
- options: List parameters including sorting, searching, page number, and page size.
";

    public static readonly string GET_MESSAGE_TYPE_BY_ID_DESCRIPTION = @"
Retrieves a message type by its unique ID.
    
Parameters:
- typeId: The unique identifier of the message type.
";

    public static readonly string CREATE_MESSAGE_TYPE_DESCRIPTION = @"
Creates a new message type.
    
Parameters:
- request: Information about the message type to be created.
";

    public static readonly string UPDATE_MESSAGE_TYPE_DESCRIPTION = @"
Updates an existing message type.
    
Parameters:
- typeId: The unique identifier of the message type.
- request: Information to update the message type.
";

    public static readonly string DELETE_MESSAGE_TYPE_DESCRIPTION = @"
Deletes a message type permanently.
    
Parameters:
- typeId: The unique identifier of the message type.
";

    #endregion
}
