namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a contact in the system as a member of a distribution list.</summary>
public class ContactPreferences : Contact
{
    /// <summary>Communication Preferences </summary>
    public RecepientPreference Preferences { get; set; } = null!;

}

public static class ContactPreferencesExtensions
{
    public static ContactPreferences ToContactPreferences(this Contact? contact) {
        if (contact == null) throw new ArgumentNullException(nameof(contact));
        var preferences = new ContactPreferences {
            Id = contact.Id,
            RecipientId = contact.RecipientId,
            Salutation = contact.Salutation,
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            FullName = contact.FullName,
            Email = contact.Email,
            PhoneNumber = contact.PhoneNumber,
            Unsubscribed = contact.Unsubscribed,
            UpdatedAt = contact.UpdatedAt,
            Preferences = new RecepientPreference() // or set to null/default as appropriate
        };
        return preferences;
    }
}