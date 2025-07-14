namespace Indice.Features.Messages.Core.Models;
/// <summary>Models a contact preference for a recipient.</summary>
public class CommunicationPreference
{
    /// <summary>The recipient correlation code.</summary>
    public required string RecipientId { get; set; }
    /// <summary>Contact's locale.</summary>
    public string? Locale { get; set; }
    /// <summary>Communication preferences per message type.</summary>
    public List<CommunicationMessageTypePreference> MessageTypeCommunicationPreferences { get; set; } = [];
}
/// <summary>Models a contact preference for a recipient.</summary>
public class CommunicationMessageTypePreference
{
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public ContactChannelKind CommunicationPreferences { get; set; } = ContactChannelKind.Any;
    /// <summary>The type details of the campaign.</summary>
    public MessageType Type { get; set; } = null!;
}