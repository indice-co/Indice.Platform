namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to create a new campaign.</summary>
public class CreateCampaignRequest : CampaignRequestBase
{
    /// <summary>Determines if a campaign is published.</summary>
    public bool Published { get; set; }
    /// <summary>Defines a list of user identifiers that constitutes the audience of the campaign.</summary>
    public List<string> RecipientIds { get; set; } = new List<string>();
    /// <summary>
    /// List of anonymous contacts not available through any of the existing contact resolvers.
    /// Use this list if recipient id is not known/available or the message will be fire and forget.
    /// </summary>
    public List<ContactAnonymous> Recipients { get; set; } = new List<ContactAnonymous>();

    internal IEnumerable<Contact> GetIncludedContacts() {
        if (RecipientIds is not null) {
            foreach (var item in RecipientIds) {
                yield return new Contact { 
                    RecipientId = item 
                };
            }
        }
        if (Recipients is not null) {
            foreach (var item in Recipients) {
                yield return item.ToContact();
            }
        }
    }
}
