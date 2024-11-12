#if NET7_0_OR_GREATER
#nullable enable
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Messages.AspNetCore.Endpoints;

internal static class DistributionListsHandlers
{
    public static async Task<Ok<ResultSet<DistributionList>>> GetDistributionLists(IDistributionListService distributionListService, [AsParameters] ListOptions options, [AsParameters] DistributionListFilter filter) {
        var lists = await distributionListService.GetList(options, filter);
        return TypedResults.Ok(lists);
    }

    public static async Task<Results<Ok<DistributionList>, NotFound>> GetDistributionListById(IDistributionListService distributionListService, Guid distributionListId) {
        var list = await distributionListService.GetById(distributionListId);
        if (list is null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(list);
    }

    public static async Task<CreatedAtRoute<DistributionList>> CreateDistributionList(IDistributionListService distributionListService, CreateDistributionListRequest request) {
        var list = await distributionListService.Create(request);
        return TypedResults.CreatedAtRoute(list, nameof(GetDistributionListById), new { distributionListId = list.Id });
    }
    public static async Task<NoContent> DeleteDistributionList(IDistributionListService distributionListService, Guid distributionListId) {
        await distributionListService.Delete(distributionListId);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> UpdateDistributionList(IDistributionListService distributionListService, Guid distributionListId, UpdateDistributionListRequest request) {
        await distributionListService.Update(distributionListId, request);
        return TypedResults.NoContent();
    }

    public static async Task<Ok<ResultSet<Contact>>> GetDistributionListContacts(IContactService contactService, Guid distributionListId, [AsParameters] ListOptions options) {
        var listOptions = new ListOptions<ContactListFilter> { Page = options.Page, Size = options.Size, Sort = options.Sort, Search = options.Search, Filter = new ContactListFilter { DistributionListId = distributionListId } };
        var contacts = await contactService.GetList(listOptions);
        return TypedResults.Ok(contacts);
    }

    public static async Task<NoContent> AddContactToDistributionList(IContactService contactService, Guid distributionListId, CreateDistributionListContactRequest request) {
        await contactService.AddToDistributionList(distributionListId, request);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> RemoveContactFromDistributionList(IContactService contactService, Guid distributionListId, Guid contactId) {
        await contactService.RemoveFromDistributionList(distributionListId, contactId);
        return TypedResults.NoContent();
    }

    #region Descriptions
    public static readonly string GET_DISTRIBUTION_LISTS_DESCRIPTION = @"
    Retrieves the list of available distribution lists based on the provided ListOptions and DistributionListFilter.

    Parameters:
    - options: List parameters used to navigate through collections. Contains parameters such as sort, search, page number, and page size.
    - filter: The filter applied to limit the results.";

    public static readonly string GET_DISTRIBUTION_LIST_BY_ID_DESCRIPTION = @"
    Retrieves a distribution list with the specified ID.

    Parameters:
    - distributionListId: The unique ID of the distribution list to retrieve.";

    public static readonly string CREATE_DISTRIBUTION_LIST_DESCRIPTION = @"
    Creates a new distribution list in the system.

    Parameters:
    - request: Contains information about the distribution list to be created.";

    public static readonly string DELETE_DISTRIBUTION_LIST_DESCRIPTION = @"
    Deletes a distribution list from the system.

    Parameters:
    - distributionListId: The unique ID of the distribution list to delete.";

    public static readonly string UPDATE_DISTRIBUTION_LIST_DESCRIPTION = @"
    Updates an existing distribution list with new details.

    Parameters:
    - distributionListId: The unique ID of the distribution list to update.
    - request: Models a request containing updated details of the distribution list.";

    public static readonly string GET_DISTRIBUTION_LIST_CONTACTS_DESCRIPTION = @"
    Retrieves contacts associated with the specified distribution list.

    Parameters:
    - distributionListId: The unique ID of the distribution list.
    - options: List parameters used to navigate through collections. Contains parameters such as sort, search, page number, and page size.";

    public static readonly string ADD_CONTACT_TO_DISTRIBUTION_LIST_DESCRIPTION = @"
    Adds a new or existing contact to the specified distribution list. If the request contains an ID, an existing contact is added; otherwise, a new contact is created.

    Parameters:
    - distributionListId: The unique ID of the distribution list.
    - request: Contains information about the contact to add to the distribution list.";

    public static readonly string REMOVE_CONTACT_FROM_DISTRIBUTION_LIST_DESCRIPTION = @"
    Removes a contact from the specified distribution list.

    Parameters:
    - distributionListId: The unique ID of the distribution list.
    - contactId: The unique ID of the contact to remove.";
    #endregion
}
#nullable disable
#endif