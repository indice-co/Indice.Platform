namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>Options used to filter the list of contacts.</summary>
public class ContactListFilter
{
    /// <summary>The id of a distribution list.</summary>
    public Guid? DistributionListId { get; set; }
    /// <summary>The recipientid associated with the contact.</summary>
    public string RecipientId { get; set; }
    /// <summary>The email for the contact to search.</summary>
    public string Email { get; set; }
    /// <summary>The phone number for the contact to search.</summary>
    public string PhoneNumber { get; set; }
}
