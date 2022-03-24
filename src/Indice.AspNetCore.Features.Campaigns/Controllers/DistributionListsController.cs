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
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route($"{ApiPrefixes.CampaignManagementEndpoints}/distribution-lists")]
    internal class DistributionListsController : ControllerBase
    {
        public DistributionListsController(IDistributionListService distributionListService) {
            DistributionListService = distributionListService ?? throw new ArgumentNullException(nameof(distributionListService));
        }

        public IDistributionListService DistributionListService { get; }

        /// <summary>
        /// Gets the list of available campaign types.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<DistributionList>))]
        public async Task<IActionResult> GetDistributionLists([FromQuery] ListOptions options) {
            var lists = await DistributionListService.GetDistributionLists(options);
            return Ok(lists);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="distributionListId"></param>
        /// <response code="200">OK</response>
        [HttpGet("{distributionListId:guid}/contacts")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<Contact>))]
        public async Task<IActionResult> GetDistributionListContacts([FromQuery] ListOptions options, [FromRoute] Guid distributionListId) {
            await Task.CompletedTask;
            return Ok();
        }

        /// <summary>
        /// Creates a new distribution list.
        /// </summary>
        /// <param name="request">Contains info about the campaign type to be created.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DistributionList))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        public async Task<IActionResult> CreateDistributionList([FromBody] CreateDistributionListRequest request) {
            var newList = await DistributionListService.CreateDistributionList(request);
            return Ok(newList);
        }
    }
}
