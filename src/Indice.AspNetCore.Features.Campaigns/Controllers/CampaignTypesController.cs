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
    [Route($"{ApiPrefixes.CampaignManagementEndpoints}/campaign-types")]
    internal class CampaignTypesController : ControllerBase
    {
        public CampaignTypesController(ICampaignService campaignService) {
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
        }

        public ICampaignService CampaignService { get; }

        /// <summary>
        /// Gets the list of available campaign types.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme)]
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<CampaignType>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> GetCampaignTypes([FromQuery] ListOptions options) {
            var campaignTypes = await CampaignService.GetCampaignTypes(options);
            return Ok(campaignTypes);
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CampaignType))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> CreateCampaignType([FromBody] UpsertCampaignTypeRequest request) {
            var campaign = await CampaignService.CreateCampaignType(request);
            return Ok(campaign);
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
        [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(CampaignType))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateCampaignType([FromRoute] Guid campaignTypeId, [FromBody] UpsertCampaignTypeRequest request) {
            var campaignType = await CampaignService.GetCampaignTypeById(campaignTypeId);
            if (campaignType is null) {
                return NotFound();
            }
            await CampaignService.UpdateCampaignType(campaignTypeId, request);
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
        public async Task<IActionResult> DeleteCampaignType([FromRoute] Guid campaignTypeId) {
            var campaignType = await CampaignService.GetCampaignTypeById(campaignTypeId);
            if (campaignType == null) {
                return NotFound();
            }
            await CampaignService.DeleteCampaignType(campaignTypeId);
            return NoContent();
        }
    }
}
