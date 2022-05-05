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
    [ApiController]
    [ApiExplorerSettings(GroupName = ApiGroups.CampaignManagementEndpoints)]
    [Authorize(AuthenticationSchemes = MessagesApi.AuthenticationScheme, Policy = MessagesApi.Policies.BeCampaignManager)]
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

        /// <summary>Gets the list of available campaign types.</summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [ProducesResponseType(typeof(ResultSet<DistributionList>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDistributionLists([FromQuery] ListOptions options) {
            var lists = await DistributionListService.GetList(options);
            return Ok(lists);
        }

        /// <summary>Gets a distribution list by it's unique id.</summary>
        /// <param name="distributionListId">The id of the message type.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{distributionListId:guid}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(DistributionList), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDistributionListById([FromRoute] Guid distributionListId) {
            var list = await DistributionListService.GetById(distributionListId);
            if (list is null) {
                return NotFound();
            }
            return Ok(list);
        }

        /// <summary>Creates a new distribution list.</summary>
        /// <param name="request">Contains info about the distribution list to be created.</param>
        /// <response code="201">Created</response>
        /// <response code="400">Bad Request</response>
        [HttpPost]
        [ProducesResponseType(typeof(DistributionList), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDistributionList([FromBody] CreateDistributionListRequest request) {
            request.CreatedBy = CreatedBy.User;
            var list = await DistributionListService.Create(request);
            return CreatedAtAction(nameof(GetDistributionListById), new { distributionListId = list.Id }, list);
        }

        /// <summary>Permanently deletes a distribution list.</summary>
        /// <param name="distributionListId">The id of the distribution list.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpDelete("{distributionListId:guid}")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteDistributionList([FromRoute] Guid distributionListId) {
            await DistributionListService.Delete(distributionListId);
            return NoContent();
        }

        /// <summary>Updates an existing distribution list.</summary>
        /// <param name="distributionListId">The id of the distribution list.</param>
        /// <param name="request">Models a request when updating a distribution list.</param>
        /// <response code="204">No Content</response>
        /// <response code="400">Bad Request</response>
        [HttpPut("{distributionListId:guid}")]
        [ProducesResponseType(typeof(DistributionList), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateDistributionList([FromRoute] Guid distributionListId, [FromBody] UpdateDistributionListRequest request) {
            await DistributionListService.Update(distributionListId, request);
            return NoContent();
        }

        /// <summary>Gets the contacts of a given distribution list.</summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <param name="distributionListId">The id of the distribution list.</param>
        /// <response code="200">OK</response>
        [HttpGet("{distributionListId:guid}/contacts")]
        [ProducesResponseType(typeof(ResultSet<Contact>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDistributionListContacts([FromRoute] Guid distributionListId, [FromQuery] ListOptions options) {
            var listOptions = new ListOptions<ContactListFilter> {
                Page = options.Page,
                Size = options.Size,
                Sort = options.Sort,
                Search = options.Search,
                Filter = new ContactListFilter {
                    DistributionListId = distributionListId
                }
            };
            var contacts = await ContactService.GetList(listOptions);
            return Ok(contacts);
        }

        /// <summary>Adds a new or existing contact in the specified distribution list.</summary>
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

        /// <summary>Removes an existing contact from the specified distribution list.</summary>
        /// <param name="distributionListId">The id of the distribution list.</param>
        /// <param name="contactId">The unique id of the contact.</param>
        /// <response code="204">No Content</response>
        /// <response code="204">Bad Request</response>
        [HttpDelete("{distributionListId:guid}/contacts/{contactId:guid}")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RemoveContactFromDistributionList([FromRoute] Guid distributionListId, [FromRoute] Guid contactId) {
            await ContactService.RemoveFromDistributionList(distributionListId, contactId);
            return NoContent();
        }
    }
}
