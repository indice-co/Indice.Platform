using System;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
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

namespace Indice.Features.Messages.AspNetCore.Controllers
{
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [ApiController]
    [Authorize(AuthenticationSchemes = MessagesApi.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route(ApiPrefixes.CampaignInboxEndpoints)]
    internal class InboxController : CampaignsControllerBase
    {
        public InboxController(
            IInboxService inboxService,
            ICampaignService campaignService,
            IMessageTypeService messageTypeService,
            IOptions<MessageInboxOptions> campaignEndpointOptions,
            Func<string, IFileService> getFileService
        ) : base(getFileService) {
            InboxService = inboxService ?? throw new ArgumentNullException(nameof(inboxService));
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            MessageTypeService = messageTypeService ?? throw new ArgumentNullException(nameof(messageTypeService));
            CampaignInboxOptions = campaignEndpointOptions?.Value ?? throw new ArgumentNullException(nameof(campaignEndpointOptions));
        }

        public IInboxService InboxService { get; }
        public ICampaignService CampaignService { get; }
        public IMessageTypeService MessageTypeService { get; }
        public MessageInboxOptions CampaignInboxOptions { get; }
        public string UserCode => User.FindFirstValue(CampaignInboxOptions.UserClaimType);

        /// <summary>
        /// Gets the list of all user messages using the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet("my/messages")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ResultSet<Message>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMessages([FromQuery] ListOptions<MessagesFilter> options) {
            var messages = await InboxService.GetList(UserCode, options);
            return Ok(messages);
        }

        /// <summary>
        /// Gets the list of available campaign types.
        /// </summary>
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

        /// <summary>
        /// Gets the message with the specified id.
        /// </summary>
        /// <param name="messageId">The id of the message.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("my/messages/{messageId:guid}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(Message), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMessageById([FromRoute] Guid messageId) {
            var message = await InboxService.GetById(messageId, UserCode);
            if (message == null) {
                return NotFound();
            }
            return Ok(message);
        }

        /// <summary>
        /// Marks the specified message as read.
        /// </summary>
        /// <param name="messageId">The id of the message.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpPut("my/messages/{messageId:guid}/read")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MarkMessageAsRead([FromRoute] Guid messageId) {
            await InboxService.MarkAsRead(messageId, UserCode);
            return NoContent();
        }

        /// <summary>
        /// Marks the specified message as deleted.
        /// </summary>
        /// <param name="messageId">The id of the message.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpDelete("my/messages/{messageId:guid}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteMessage([FromRoute] Guid messageId) {
            await InboxService.MarkAsDeleted(messageId, UserCode);
            return NoContent();
        }

        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("messages/cta/{trackingCode}")]
        public async Task<IActionResult> Track([FromRoute] Base64Id trackingCode) {
            var campaignId = trackingCode.Id;
            var campaign = await CampaignService.GetById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            await CampaignService.UpdateHit(trackingCode.Id);
            return Redirect(campaign.ActionLink.Href);
        }

        /// <summary>
        /// Gets the attachment associated with a campaign.
        /// </summary>
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
}
