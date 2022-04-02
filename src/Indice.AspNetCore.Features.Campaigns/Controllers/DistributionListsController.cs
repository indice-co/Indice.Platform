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
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [Route($"{ApiPrefixes.CampaignManagementEndpoints}/distribution-lists")]
    internal class DistributionListsController : ControllerBase
    {
        public DistributionListsController(
            IDistributionListService distributionListService,
            IContactService contactService
        ) {
            DistributionListService = distributionListService ?? throw new ArgumentNullException(nameof(distributionListService));
            ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
        }

        public IDistributionListService DistributionListService { get; }
        public IContactService ContactService { get; }

        /// <summary>
        /// Gets the list of available campaign types.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [ProducesResponseType(typeof(ResultSet<DistributionList>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDistributionLists([FromQuery] ListOptions options) {
            var lists = await DistributionListService.GetList(options);
            return Ok(lists);
        }

        /// <summary>
        /// Gets the contacts of a given distribution list.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <param name="distributionListId">The id of the distribution list.</param>
        /// <response code="200">OK</response>
        [HttpGet("{distributionListId:guid}/contacts")]
        [ProducesResponseType(typeof(ResultSet<Contact>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDistributionListContacts([FromRoute] Guid distributionListId, [FromQuery] ListOptions options) {
            var contacts = await DistributionListService.GetContactsList(distributionListId, options);
            return Ok(contacts);
        }

        /// <summary>
        /// Adds a new or existing contact in the specified distribution list.
        /// </summary>
        /// <param name="distributionListId">The id of the distribution list.</param>
        /// <param name="request">Contains info about the contact to be assigned to the distribution list.</param>
        /// <response code="204">No Content</response>
        /// <response code="204">Bad Request</response>
        [HttpPost("{distributionListId:guid}/contacts")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddContactToDistributionList([FromRoute] Guid distributionListId, [FromBody] CreateDistributionListContactRequest request) {
            await ContactService.AddToDistributionList(distributionListId, request);
            return NoContent();
        }

        /// <summary>
        /// Creates a new distribution list.
        /// </summary>
        /// <param name="request">Contains info about the distribution list to be created.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [HttpPost]
        [ProducesResponseType(typeof(DistributionList), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDistributionList([FromBody] CreateDistributionListRequest request) {
            var list = await DistributionListService.Create(request);
            return Ok(list);
        }
    }
}
