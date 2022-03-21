using System;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
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
    [Route($"{ApiPrefixes.InboxApi}/my/messages")]
    internal class InboxController : ControllerBase
    {
        public InboxController(
            IInboxService inboxService,
            IOptions<CampaignEndpointOptions> campaignEndpointOptions
        ) {
            InboxService = inboxService ?? throw new ArgumentNullException(nameof(inboxService));
            CampaignEndpointOptions = campaignEndpointOptions?.Value ?? throw new ArgumentNullException(nameof(campaignEndpointOptions));
        }

        public IInboxService InboxService { get; }
        public CampaignEndpointOptions CampaignEndpointOptions { get; }
        public string UserCode => User.FindFirstValue(CampaignEndpointOptions.UserClaimType);

        /// <summary>
        /// Gets the list of all user messages using the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<Message>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetMessages([FromQuery] ListOptions<MessagesFilter> options) {
            var messages = await InboxService.GetMessages(UserCode, options);
            return Ok(messages);
        }

        /// <summary>
        /// Gets the message with the specified id.
        /// </summary>
        /// <param name="messageId">The id of the message.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{messageId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Message))]
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
        [HttpPut("{messageId:guid}/read")]
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
        [HttpDelete("{messageId:guid}")]
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
    }
}
