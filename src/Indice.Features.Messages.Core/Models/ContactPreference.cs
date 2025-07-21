namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a contact in the system as a member of a distribution list.</summary>
public class ContactPreferences : Contact
{
    /// <summary>Communication Preferences </summary>
    public RecepientPreference Preferences { get; set; } = null!;

}