#if NET7_0_OR_GREATER

using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using System;

namespace Indice.Features.Messages.AspNetCore.Endpoints;



public static class DistributionListsHandlers
{
    public static async Task<IResult> GetDistributionLists(IDistributionListService distributionListService, [AsParameters] ListOptions options, [AsParameters] DistributionListFilter filter) {
        var lists = await distributionListService.GetList(options, filter);
        return Results.Ok(lists);
    }

    public static async Task<IResult> GetDistributionListById(IDistributionListService distributionListService, Guid distributionListId) {
        var list = await distributionListService.GetById(distributionListId);
        if (list is null) {
            return Results.NotFound();
        }
        return Results.Ok(list);
    }

    public static async Task<IResult> CreateDistributionList(IDistributionListService distributionListService, CreateDistributionListRequest request) {
        var list = await distributionListService.Create(request);
        return Results.CreatedAtRoute("GetDistributionListById", new { distributionListId = list.Id }, list);
    }

    public static async Task<IResult> DeleteDistributionList(IDistributionListService distributionListService, Guid distributionListId) {
        await distributionListService.Delete(distributionListId);
        return Results.NoContent();
    }

    public static async Task<IResult> UpdateDistributionList(IDistributionListService distributionListService, Guid distributionListId, UpdateDistributionListRequest request) {
        await distributionListService.Update(distributionListId, request);
        return Results.NoContent();
    }

    public static async Task<IResult> GetDistributionListContacts(IContactService contactService, Guid distributionListId, [AsParameters] ListOptions options) {
        var listOptions = new ListOptions<ContactListFilter> { Page = options.Page, Size = options.Size, Sort = options.Sort, Search = options.Search, Filter = new ContactListFilter { DistributionListId = distributionListId } };
        var contacts = await contactService.GetList(listOptions);
        return Results.Ok(contacts);
    }

    public static async Task<IResult> AddContactToDistributionList(IContactService contactService, Guid distributionListId, CreateDistributionListContactRequest request) {
        await contactService.AddToDistributionList(distributionListId, request);
        return Results.NoContent();
    }

    public static async Task<IResult> RemoveContactFromDistributionList(IContactService contactService, Guid distributionListId, Guid contactId) {
        await contactService.RemoveFromDistributionList(distributionListId, contactId);
        return Results.NoContent();
    }
}

#endif