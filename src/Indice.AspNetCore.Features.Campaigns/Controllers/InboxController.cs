using System;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Controllers
{
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [ApiController]
    [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route(ApiPrefixes.CampaignInboxEndpoints)]
    internal class InboxController : CampaignsControllerBase
    {
        public InboxController(
            IInboxService inboxService,
            ICampaignService campaignService,
            IOptions<CampaignInboxOptions> campaignEndpointOptions,
            Func<string, IFileService> getFileService
        ) : base(getFileService) {
            InboxService = inboxService ?? throw new ArgumentNullException(nameof(inboxService));
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
            CampaignInboxOptions = campaignEndpointOptions?.Value ?? throw new ArgumentNullException(nameof(campaignEndpointOptions));
        }

        public IInboxService InboxService { get; }
        public ICampaignService CampaignService { get; }
        public CampaignInboxOptions CampaignInboxOptions { get; }
        public string UserCode => User.FindFirstValue(CampaignInboxOptions.UserClaimType);

        /// <summary>
        /// Gets the list of all user messages using the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet("my/messages")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<UserMessage>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetMessages([FromQuery] ListOptions<UserMessageFilter> options) {
            var messages = await InboxService.GetUserMessages(UserCode, options);
            return Ok(messages);
        }

        /// <summary>
        /// Gets the list of available campaign types.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme)]
        [HttpGet("message-types")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<CampaignType>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> GetMessageTypes([FromQuery] ListOptions options) {
            var campaignTypes = await CampaignService.GetCampaignTypes(options);
            return Ok(campaignTypes);
        }

        /// <summary>
        /// Gets the message with the specified id.
        /// </summary>
        /// <param name="messageId">The id of the message.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("my/messages/{messageId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserMessage))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetMessageById([FromRoute] Guid messageId) {
            var message = await InboxService.GetMessageById(messageId, UserCode);
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
        /// <response code="404">Not Found</response>
        [HttpPut("my/messages/{messageId:guid}/read")]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(void))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(void))]
        public async Task<IActionResult> MarkMessageAsRead([FromRoute] Guid messageId) {
            var message = await InboxService.GetMessageById(messageId, UserCode);
            if (message == null) {
                return NotFound();
            }
            if (message.IsRead) {
                ModelState.AddModelError(nameof(message), "This message is already read.");
                return BadRequest(new ValidationProblemDetails(ModelState));
            }
            await InboxService.MarkMessageAsRead(messageId, UserCode);
            return NoContent();
        }

        /// <summary>
        /// Marks the specified message as deleted.
        /// </summary>
        /// <param name="messageId">The id of the message.</param>
        /// <response code="204">No Content</response>
        [HttpDelete("my/messages/{messageId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(void))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(void))]
        public async Task<IActionResult> DeleteMessage([FromRoute] Guid messageId) {
            var message = await InboxService.GetMessageById(messageId, UserCode);
            if (message == null) {
                return NotFound();
            }
            await InboxService.MarkMessageAsDeleted(messageId, UserCode);
            return NoContent();
        }

        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("messages/cta/{trackingCode}")]
        public async Task<IActionResult> Track([FromRoute] Base64Id trackingCode) {
            var campaignId = trackingCode.Id;
            // TODO: Add a method on campaign service that returns the cta link.
            var campaign = await CampaignService.GetCampaignById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            await CampaignService.UpdateCampaignVisit(campaignId);
            return Redirect(campaign.ActionUrl);
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "v" })]
        public async Task<IActionResult> GetMessageAttachment([FromRoute] Base64Id fileGuid, [FromRoute] string format) => await GetFile("campaigns", fileGuid, format);
    }
}
