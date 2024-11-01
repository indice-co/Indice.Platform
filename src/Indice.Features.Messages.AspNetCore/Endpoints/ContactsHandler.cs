#if NET7_0_OR_GREATER

using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;

namespace Indice.Features.Messages.AspNetCore.Endpoints;



internal static class ContactsHandler
{
    public static async Task<IResult> GetContacts(
        IContactService contactService,
        IContactResolver contactResolver,
        [AsParameters] ListOptions options,
        [AsParameters] ContactListFilter filter,
        bool resolve) {

        ResultSet<Contact> contacts;
        if (resolve) {
            contacts = await contactResolver.Find(new ListOptions {
                Page = options.Page,
                Search = options.Search,
                Size = options.Size,
                Sort = options.Sort
            });
        } else {
            contacts = await contactService.GetList(ListOptions.Create(options, filter));
        }
        return Results.Ok(contacts);
    }

    public static async Task<IResult> GetContactById(IContactService contactService, Guid contactId) {
        var contact = await contactService.GetById(contactId);
        return contact != null ? Results.Ok(contact) : Results.NotFound();
    }

    public static async Task<IResult> CreateContact(IContactService contactService, CreateContactRequest request) {
        var contact = await contactService.Create(request);
        return Results.Ok(contact);
    }

    public static async Task<IResult> UpdateContact(IContactService contactService, Guid contactId, UpdateContactRequest request) {
        await contactService.Update(contactId, request);
        return Results.NoContent();
    }
}

#endif