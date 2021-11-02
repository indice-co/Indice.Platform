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
    [ApiController]
    [ApiExplorerSettings(GroupName = "campaigns")]
    [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme, Policy = CampaignsApi.Policies.BeCampaignsManager)]
    [Route("api/campaigns")]
    internal class CampaignsController : ControllerBase
    {
        public const string Name = "Campaigns";

        public CampaignsController(ICampaignService campaignService) {
            CampaignService = campaignService ?? throw new ArgumentNullException(nameof(campaignService));
        }

        public ICampaignService CampaignService { get; }

        /// <summary>
        /// Gets the list of all campaigns using the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <response code="200">OK</response>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<Campaign>))]
        public async Task<IActionResult> GetCampaigns([FromQuery] ListOptions options) {
            var campaigns = await CampaignService.GetCampaigns(options);
            return Ok(campaigns);
        }

        /// <summary>
        /// Gets a campaign with the specified id.
        /// </summary>
        /// <param name="campaignId">The id of the campaign.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{campaignId:guid}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CampaignDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetCampaignById([FromRoute] Guid campaignId) {
            var campaign = await CampaignService.GetCampaignById(campaignId);
            if (campaign is null) {
                return NotFound();
            }
            return Ok(campaign);
        }
    }
}