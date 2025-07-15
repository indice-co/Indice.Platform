namespace Indice.Features.Messages.Core.Models;
/// <summary>Models a contact preference for a recipient.</summary>
public class CommunicationPreference
{
    /// <summary>Contact's locale.</summary>
    public string? Locale { get; set; }
    /// <summary>Communication preferences per message type.</summary>
    public List<CommunicationMessageTypePreference> MessageTypeCommunicationPreferences { get; set; } = [];
}
/// <summary>Models a contact preference for a recipient.</summary>
public class CommunicationMessageTypePreference
{
    /// <summary>The name of a campaign type.</summary>
    public string? Name { get; set; }
    /// <summary>The alias of a campaign type.</summary>
    public string? Alias { get; set; }
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public ContactChannelKind CommunicationPreferences { get; set; } = ContactChannelKind.Any;
}