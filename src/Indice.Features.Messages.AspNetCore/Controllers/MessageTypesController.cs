using System.Net.Mime;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Messages.AspNetCore.Controllers;

/// <response code="401">Unauthorized</response>
/// <response code="403">Forbidden</response>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CampaignManagementEndpoints)]
[Authorize(AuthenticationSchemes = MessagesApi.AuthenticationScheme, Policy = MessagesApi.Policies.BeCampaignManager)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[Route($"{ApiPrefixes.CampaignManagementEndpoints}/message-types")]
internal class MessageTypesController : ControllerBase
{
    public MessageTypesController(
        ICampaignService campaignService,
        IMessageTypeService messageTypeService
    ) {
        CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
        MessageTypeService = messageTypeService ?? throw new ArgumentNullException(nameof(messageTypeService));
    }

    public ICampaignService CampaignService { get; }
    public IMessageTypeService MessageTypeService { get; }

    /// <summary>Gets the list of available message types.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <response code="200">OK</response>
    [HttpGet]
    [ProducesResponseType(typeof(ResultSet<MessageType>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessageTypes([FromQuery] ListOptions options) {
        var messageTypes = await MessageTypeService.GetList(options);
        return Ok(messageTypes);
    }

    /// <summary>Gets a message type by it's unique id.</summary>
    /// <param name="typeId">The id of the message type.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [HttpGet("{typeId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(MessageType), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageTypeById([FromRoute] Guid typeId) {
        var messageType = await MessageTypeService.GetById(typeId);
        if (messageType is null) {
            return NotFound();
        }
        return Ok(messageType);
    }

    /// <summary>Creates a new message type.</summary>
    /// <param name="request">Contains info about the message type to be created.</param>
    /// <response code="201">Created</response>
    /// <response code="400">Bad Request</response>
    [HttpPost]
    [ProducesResponseType(typeof(MessageType), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMessageType([FromBody] CreateMessageTypeRequest request) {
        var messageType = await MessageTypeService.Create(request);
        return CreatedAtAction(nameof(GetMessageTypeById), new { typeId = messageType.Id }, messageType);
    }

    /// <summary>Updates an existing message type.</summary>
    /// <param name="typeId">The id of the message type.</param>
    /// <param name="request">Contains info about the message type to update.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    [HttpPut("{typeId:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMessageType([FromRoute] Guid typeId, [FromBody] UpdateMessageTypeRequest request) {
        await MessageTypeService.Update(typeId, request);
        return NoContent();
    }

    /// <summary>Permanently deletes a message type.</summary>
    /// <param name="typeId">The id of the message type.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    [HttpDelete("{typeId:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteMessageType([FromRoute] Guid typeId) {
        await MessageTypeService.Delete(typeId);
        return NoContent();
    }
}
