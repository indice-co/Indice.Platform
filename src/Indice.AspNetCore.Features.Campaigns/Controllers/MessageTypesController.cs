using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.AspNetCore.Features.Campaigns.Controllers
{
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [ApiController]
    [ApiExplorerSettings(GroupName = "campaigns")]
    [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme, Policy = CampaignsApi.Policies.BeCampaignsManager)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
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

        /// <summary>
        /// Gets the list of available message types.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<MessageType>))]
        public async Task<IActionResult> GetMessageTypes([FromQuery] ListOptions options) {
            var messageTypes = await MessageTypeService.GetList(options);
            return Ok(messageTypes);
        }

        /// <summary>
        /// Creates a new message type.
        /// </summary>
        /// <param name="request">Contains info about the message type to be created.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageType))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> CreateMessageType([FromBody] UpsertMessageTypeRequest request) {
            var messageType = await MessageTypeService.Create(request);
            return Ok(messageType);
        }

        /// <summary>
        /// Updates an existing message type.
        /// </summary>
        /// <param name="campaignTypeId">The id of the message type.</param>
        /// <param name="request">Contains info about the message type to update.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpPut("{campaignTypeId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(MessageType))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateMessageType([FromRoute] Guid campaignTypeId, [FromBody] UpsertMessageTypeRequest request) {
            var updated = await MessageTypeService.Update(campaignTypeId, request);
            if (!updated) {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>
        /// Permanently deletes a message type.
        /// </summary>
        /// <param name="campaignTypeId">The id of the message type.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(void))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteMessageType([FromRoute] Guid campaignTypeId) {
            var deleted = await MessageTypeService.Delete(campaignTypeId);
            if (!deleted) {
                return NotFound();
            }
            return NoContent();
        }
    }
}
