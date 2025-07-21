namespace Indice.Features.Messages.Core.Models;
/// <summary>Models a contact preference for a recipient.</summary>
public class RecepientPreference
{
    /// <summary>Contact's locale.</summary>
    public string? Locale { get; set; }
    /// <summary>Indicates if user accepted conset to receive email.</summary>
    public bool ConsentCommercial { get; set; }
    /// <summary>Indicates if user accepted conset to receive email.</summary>
    public DateTimeOffset? ConsentCommercialDate { get; set; }
    /// <summary>Communication preferences per message type.</summary>
    public List<RecepientPreferenceCommunication> CommunicationPreferences { get; set; } = [];
}
/// <summary>Models a contact preference for a recipient.</summary>
public class RecepientPreferenceCommunication
{
    /// <summary>The name of a campaign type.</summary>
    public string? Name { get; set; }
    /// <summary>The alias of a campaign type.</summary>
    public string? Alias { get; set; }
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public List<ContactChannelKind> Channels { get; set; } = [ContactChannelKind.Any];
}