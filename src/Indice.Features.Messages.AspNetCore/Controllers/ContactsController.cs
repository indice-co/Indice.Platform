using System.Net.Mime;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Messages.AspNetCore.Controllers;

/// <response code="401">Unauthorized</response>
/// <response code="403">Forbidden</response>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CampaignManagementEndpoints)]
[Authorize(AuthenticationSchemes = MessagesApi.AuthenticationScheme, Policy = MessagesApi.Policies.BeCampaignManager)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[Route($"{ApiPrefixes.CampaignManagementEndpoints}/contacts")]
internal class ContactsController(IContactService contactService, IContactResolver contactResolver) : ControllerBase
{
    public IContactService ContactService { get; } = contactService ?? throw new ArgumentNullException(nameof(contactService));
    public IContactResolver ContactResolver { get; } = contactResolver ?? throw new ArgumentNullException(nameof(contactResolver));

    /// <summary>Gets the list of all contacts using the provided <see cref="ListOptions"/>.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <param name="filter"></param>
    /// <param name="resolve"></param>
    /// <response code="200">OK</response>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ResultSet<Contact>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContacts([FromQuery] ListOptions options, [FromQuery] ContactListFilter filter, [FromQuery] bool resolve) {
        ResultSet<Contact> contacts;
        if (resolve) {
            contacts = await ContactResolver.Find(new ListOptions {
                Page = options.Page,
                Search = options.Search,
                Size = options.Size,
                Sort = options.Sort
            });
        } else {
            contacts = await ContactService.GetList(ListOptions.Create(options, filter));
        }
        return Ok(contacts);
    }

    /// <summary>Gets the specified contact by it's unique id.</summary>
    /// <param name="contactId">The unique id of the contact.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [HttpGet("{contactId:guid}")]
    [ProducesResponseType(typeof(Contact), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContactById([FromRoute] Guid contactId) {
        var contact = await ContactService.GetById(contactId);
        if (contact is null) {
            return NotFound();
        }
        return Ok(contact);
    }

    /// <summary>Creates a new contact in the store.</summary>
    /// <param name="request">The request model used to create a new contact.</param>
    /// <response code="200">OK</response>
    [HttpPost]
    [ProducesResponseType(typeof(MessageType), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContact([FromBody] CreateContactRequest request) {
        var contact = await ContactService.Create(request);
        return Ok(contact);
    }

    /// <summary>Updates the specified contact in the store.</summary>
    /// <param name="contactId">The unique id of the contact.</param>
    /// <param name="request">The request model used to update a new contact.</param>
    /// <response code="204">No Content</response>
    [HttpPut("{contactId:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateContact([FromRoute] Guid contactId, [FromBody] UpdateContactRequest request) {
        await ContactService.Update(contactId, request);
        return NoContent();
    }
}
