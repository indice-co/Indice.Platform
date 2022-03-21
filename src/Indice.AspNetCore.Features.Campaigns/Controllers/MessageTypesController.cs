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
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route($"{ApiPrefixes.ManagementApi}/message-types")]
    internal class MessageTypesController : ControllerBase
    {
        public MessageTypesController(ICampaignService campaignService) {
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
        }

        public ICampaignService CampaignService { get; }

        /// <summary>
        /// Gets the list of available campaign types.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme)]
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<MessageType>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> GetMessageTypes([FromQuery] ListOptions options) {
            var messageTypes = await CampaignService.GetMessageTypes(options);
            return Ok(messageTypes);
        }

        /// <summary>
        /// Creates a new campaign type.
        /// </summary>
        /// <param name="request">Contains info about the campaign type to be created.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme, Policy = CampaignsApi.Policies.BeCampaignsManager)]
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MessageType))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> CreateMessageType([FromBody] UpsertMessageTypeRequest request) {
            var messageType = await CampaignService.CreateMessageType(request);
            return Ok(messageType);
        }

        /// <summary>
        /// Updates an existing campaign type.
        /// </summary>
        /// <param name="campaignTypeId">The id of the campaign type.</param>
        /// <param name="request">Contains info about the campaign type to update.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme, Policy = CampaignsApi.Policies.BeCampaignsManager)]
        [HttpPut("{campaignTypeId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(MessageType))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateMessageType([FromRoute] Guid campaignTypeId, [FromBody] UpsertMessageTypeRequest request) {
            var messageType = await CampaignService.GetMessageTypeById(campaignTypeId);
            if (messageType is null) {
                return NotFound();
            }
            await CampaignService.UpdateMessageType(campaignTypeId, request);
            return NoContent();
        }

        /// <summary>
        /// Permanently deletes a campaign type.
        /// </summary>
        /// <param name="campaignTypeId">The id of the campaign type.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme, Policy = CampaignsApi.Policies.BeCampaignsManager)]
        [HttpDelete]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(void))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteMessageType([FromRoute] Guid campaignTypeId) {
            var messageType = await CampaignService.GetMessageTypeById(campaignTypeId);
            if (messageType == null) {
                return NotFound();
            }
            await CampaignService.DeleteMessageType(campaignTypeId);
            return NoContent();
        }
    }
}
