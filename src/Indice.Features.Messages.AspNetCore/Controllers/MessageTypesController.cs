using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Messages.AspNetCore.Controllers
{
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [ApiController]
    [ApiExplorerSettings(GroupName = "messages")]
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

        /// <summary>
        /// Gets the list of available message types.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [ProducesResponseType(typeof(ResultSet<MessageType>), StatusCodes.Status200OK)]
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
        [ProducesResponseType(typeof(MessageType), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
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
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMessageType([FromRoute] Guid campaignTypeId, [FromBody] UpsertMessageTypeRequest request) {
            await MessageTypeService.Update(campaignTypeId, request);
            return NoContent();
        }

        /// <summary>
        /// Permanently deletes a message type.
        /// </summary>
        /// <param name="campaignTypeId">The id of the message type.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpDelete]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteMessageType([FromRoute] Guid campaignTypeId) {
            await MessageTypeService.Delete(campaignTypeId);
            return NoContent();
        }
    }
}
