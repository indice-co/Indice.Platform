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
[Route($"{ApiPrefixes.CampaignManagementEndpoints}/message-senders")]
internal class MessageSendersController : ControllerBase
{
    public MessageSendersController(
        ICampaignService campaignService,
        IMessageSenderService messageSenderService
    ) {
        CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
        MessageSenderService = messageSenderService ?? throw new ArgumentNullException(nameof(messageSenderService));
    }

    public ICampaignService CampaignService { get; }
    public IMessageSenderService MessageSenderService { get; }

    /// <summary>Gets the list of available message senders.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <param name="filter">The filter applied to the results.</param>
    /// <response code="200">OK</response>
    [HttpGet]
    [ProducesResponseType(typeof(ResultSet<MessageSender>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessageSenders([FromQuery] ListOptions options, [FromQuery] MessageSenderListFilter filter) {
        var messageSenders = await MessageSenderService.GetList(options, filter);
        return Ok(messageSenders);
    }

    /// <summary>Gets a message sender by it's unique id.</summary>
    /// <param name="senderId">The id of the message sender.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [HttpGet("{senderId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(MessageSender), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageSenderById([FromRoute] Guid senderId) {
        var messageSender = await MessageSenderService.GetById(senderId);
        if (messageSender is null) {
            return NotFound();
        }
        return Ok(messageSender);
    }

    /// <summary>Creates a new message sender.</summary>
    /// <param name="request">Contains info about the message sender to be created.</param>
    /// <response code="201">Created</response>
    /// <response code="400">Bad Request</response>
    [HttpPost]
    [ProducesResponseType(typeof(MessageSender), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMessageSender([FromBody] CreateMessageSenderRequest request) {
        var messageSender = await MessageSenderService.Create(request);
        return CreatedAtAction(nameof(GetMessageSenderById), new { senderId = messageSender.Id }, messageSender);
    }

    /// <summary>Updates an existing message sender.</summary>
    /// <param name="senderId">The id of the message sender.</param>
    /// <param name="request">Contains info about the message sender to update.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    [HttpPut("{senderId:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMessageSender([FromRoute] Guid senderId, [FromBody] UpdateMessageSenderRequest request) {
        await MessageSenderService.Update(senderId, request);
        return NoContent();
    }

    /// <summary>Permanently deletes a message sender.</summary>
    /// <param name="senderId">The id of the message sender.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    [HttpDelete("{senderId:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteMessageSender([FromRoute] Guid senderId) {
        await MessageSenderService.Delete(senderId);
        return NoContent();
    }
}
