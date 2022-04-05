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

namespace Indice.AspNetCore.Features.Campaigns.Controllers
{
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    [ApiController]
    [ApiExplorerSettings(GroupName = "campaigns")]
    [Authorize(AuthenticationSchemes = CampaignsApi.AuthenticationScheme, Policy = CampaignsApi.Policies.BeCampaignsManager)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [Route($"{ApiPrefixes.CampaignManagementEndpoints}/contacts")]
    internal class ContactsController : ControllerBase
    {
        public ContactsController(IContactService contactService) {
            ContactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
        }

        public IContactService ContactService { get; }

        /// <summary>
        /// Gets the list of all contacts using the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ResultSet<Contact>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCampaigns([FromQuery] ListOptions options) {
            var contacts = await ContactService.GetList(options);
            return Ok(contacts);
        }

        [HttpPost]
        [ProducesResponseType(typeof(MessageType), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateContact([FromBody] CreateContactRequest request) {
            var contact = await ContactService.Create(request);
            return Ok(contact);
        }
    }
}
