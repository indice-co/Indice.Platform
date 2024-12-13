namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to update an existing contact.</summary>
public class UpdateContactRequest
{
    /// <summary>Contact salutation (Mr, Mrs etc).</summary>
    public string? Salutation { get; set; }
    /// <summary>The first name.</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name.</summary>
    public string? LastName { get; set; }
    /// <summary>The full name.</summary>
    public string? FullName { get; set; }
    /// <summary>The email.</summary>
    public string? Email { get; set; }
    /// <summary>The phone number.</summary>
    public string? PhoneNumber { get; set; }
    /// <summary>Contact's locale.</summary>
    public string? Locale { get; set; }
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public ContactCommunicationChannelKind CommunicationPreferences { get; set; } = ContactCommunicationChannelKind.Any;
    /// <summary>Indicates if user accepted conset to receive email.</summary>
    public bool ConsentCommercial { get; set; }
    /// <summary>The id of the distribution list.</summary>
    public Guid? DistributionListId { get; set; }
}
