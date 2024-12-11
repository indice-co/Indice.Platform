using System.Net.Mime;
using System.Security.Claims;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.AspNetCore.Controllers;

/// <response code="401">Unauthorized</response>
/// <response code="403">Forbidden</response>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.MessageInboxEndpoints)]
[Authorize(AuthenticationSchemes = MessagesApi.AuthenticationScheme)]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[Route(ApiPrefixes.MessageInboxEndpoints)]
internal class MyMessagesController(
    IMessageService messageService,
    ICampaignService campaignService,
    IMessageTypeService messageTypeService,
    IOptions<MessageInboxOptions> campaignEndpointOptions,
    IFileServiceFactory fileServiceFactory
    ) : CampaignsControllerBase(fileServiceFactory)
{
    public IMessageService MessageService { get; } = messageService ?? throw new ArgumentNullException(nameof(messageService));
    public ICampaignService CampaignService { get; } = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
    public IMessageTypeService MessageTypeService { get; } = messageTypeService ?? throw new ArgumentNullException(nameof(messageTypeService));
    public MessageInboxOptions CampaignInboxOptions { get; } = campaignEndpointOptions?.Value ?? throw new ArgumentNullException(nameof(campaignEndpointOptions));
    public string? UserCode => User.FindFirstValue(CampaignInboxOptions.UserClaimType);

    /// <summary>Gets the list of all user messages using the provided <see cref="ListOptions"/>.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <param name="filter"></param>
    /// <response code="200">OK</response>
    [HttpGet("my/messages")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ResultSet<Message>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages([FromQuery] ListOptions options, [FromQuery] MessagesFilter filter) {
        var messages = await MessageService.GetList(UserCode, ListOptions.Create(options, filter));
        return Ok(messages);
    }

    /// <summary>Gets the list of available campaign types.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <response code="200">OK</response>
    [Authorize(AuthenticationSchemes = MessagesApi.AuthenticationScheme)]
    [HttpGet("messages/types")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ResultSet<MessageType>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInboxMessageTypes([FromQuery] ListOptions options) {
        var campaignTypes = await MessageTypeService.GetList(options);
        return Ok(campaignTypes);
    }

    /// <summary>Gets the message with the specified id.</summary>
    /// <param name="messageId">The id of the message.</param>
    /// <param name="channel">The channel of the message.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [HttpGet("my/messages/{messageId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageById([FromRoute] Guid messageId, [FromQuery] MessageChannelKind? channel) {
        var message = await MessageService.GetById(messageId, UserCode, channel);
        if (message == null) {
            return NotFound();
        }
        return Ok(message);
    }

    /// <summary>Marks the specified message as read.</summary>
    /// <param name="messageId">The id of the message.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    [HttpPut("my/messages/{messageId:guid}/read")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkMessageAsRead([FromRoute] Guid messageId) {
        await MessageService.MarkAsRead(messageId, UserCode);
        return NoContent();
    }

    /// <summary>Marks the specified message as deleted.</summary>
    /// <param name="messageId">The id of the message.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    [HttpDelete("my/messages/{messageId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMessage([FromRoute] Guid messageId) {
        await MessageService.MarkAsDeleted(messageId, UserCode);
        return NoContent();
    }

    /// <summary>Gets the attachment associated with a campaign.</summary>
    /// <param name="fileGuid">Contains the photo's Id.</param>
    /// <param name="format">Contains the format of the uploaded attachment extension.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("messages/attachments/{fileGuid}.{format}")]
    [Produces(MediaTypeNames.Application.Octet)]
    [ProducesResponseType(typeof(IFormFile), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "v" })]
    public async Task<IActionResult> GetMessageAttachment([FromRoute] Base64Id fileGuid, [FromRoute] string format) => await GetFile("campaigns", fileGuid, format);
}
