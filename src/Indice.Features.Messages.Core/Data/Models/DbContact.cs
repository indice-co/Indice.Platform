using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Contact entity.</summary>
public class DbContact
{
    /// <summary>The unique id of the contact.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The recipient correlation code.</summary>
    public string RecipientId { get; set; }
    /// <summary>Contact salutation (Mr, Mrs etc).</summary>
    public string Salutation { get; set; }
    /// <summary>The first name.</summary>
    public string FirstName { get; set; }
    /// <summary>The last name.</summary>
    public string LastName { get; set; }
    /// <summary>The full name.</summary>
    public string FullName { get; set; }
    /// <summary>The email.</summary>
    public string Email { get; set; }
    /// <summary>The phone number.</summary>
    public string PhoneNumber { get; set; }
    /// <summary>Contact's locale.</summary>
    public string Locale { get; set; }
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public ContactCommunicationChannelKind CommunicationPreferences { get; set; } = ContactCommunicationChannelKind.Any;
    /// <summary>Indicates if user accepted conset to receive email.</summary>
    public bool ConsentCommercial { get; set; }
    /// <summary>Indicates when contact info were last updated.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
    /// <summary>Contact - Distribution list join entity type.</summary>
    public List<DbDistributionListContact> DistributionListContacts { get; set; } = [];
}
