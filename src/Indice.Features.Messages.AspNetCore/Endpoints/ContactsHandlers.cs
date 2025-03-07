using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class ContactsHandlers
{
    public static async Task<Ok<ResultSet<Contact>>> GetContacts(
        IContactService contactService,
        IContactResolver contactResolver,
        [AsParameters] ListOptions options,
        [AsParameters] ContactListFilter filter,
        bool? resolve) {

        ResultSet<Contact> contacts;
        if (resolve == true) {
            contacts = await contactResolver.Find(new ListOptions {
                Page = options.Page,
                Search = options.Search,
                Size = options.Size,
                Sort = options.Sort
            });
        } else {
            contacts = await contactService.GetList(ListOptions.Create(options, filter));
        }
        return TypedResults.Ok(contacts);
    }

    public static async Task<Results<Ok<Contact>, NotFound>> GetContactById(IContactService contactService, Guid contactId) {
        var contact = await contactService.GetById(contactId);
        if (contact is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(contact);
    }

    public static async Task<Ok<Contact>> CreateContact(IContactService contactService, CreateContactRequest request) {
        var contact = await contactService.Create(request);
        return TypedResults.Ok(contact);
    }

    public static async Task<NoContent> UpdateContact(IContactService contactService, Guid contactId, UpdateContactRequest request) {
        await contactService.Update(contactId, request);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound, ValidationProblem>> RefreshContact(IContactService contactService, IContactResolver contactResolver, string recipientId) {

        if (string.IsNullOrWhiteSpace(recipientId))
            return TypedResults.ValidationProblem(ValidationErrors.AddError(nameof(recipientId), "Recipient cannot be null"));

        var resolvedContact = await contactResolver.Resolve(recipientId);
        if (resolvedContact is null) {
            return TypedResults.NotFound();
        }

        var contact = await contactService.FindByRecipientId(recipientId);
        if (contact is null) {
            await contactService.Create(Mapper.ToCreateContactRequest(resolvedContact));
            return TypedResults.NoContent();
        }

        resolvedContact.Id = contact.Id;
        await contactService.Update(contact.Id!.Value, Mapper.ToUpdateContactRequest(resolvedContact));
        return TypedResults.NoContent();
    }

    #region Descriptions
    public static readonly string GET_CONTACTS_DESCRIPTION = @"
Retrieves the list of all contacts using the provided ListOptions.

Parameters:
- options: List parameters used to navigate through collections, including sort, search, page number, and page size.
- resolve: Determines whether to use the contact resolver service for additional processing; set to true to resolve contacts, or false to retrieve directly from contactService.
";

    public static readonly string GET_CONTACT_BY_ID_DESCRIPTION = @"
Retrieves a contact by its unique ID.

Parameters:
- contactId: The unique ID of the contact to retrieve.
";

    public static readonly string CREATE_CONTACT_DESCRIPTION = @"
Creates a new contact in the store.

Parameters:
- request: The request model used to create a new contact.
";

    public static readonly string UPDATE_CONTACT_DESCRIPTION = @"
Updates an existing contact in the store.

Parameters:
- contactId: The unique ID of the contact to update.
- request: The request model used to update the contact.
";
    public static readonly string REFRESH_CONTACT_DESCRIPTION = @"
Updates an existing contact in the store or adds a new contact with data from an external system.

Parameters:
- recepientId: The unique ID of the recepient.
";


    #endregion
}
