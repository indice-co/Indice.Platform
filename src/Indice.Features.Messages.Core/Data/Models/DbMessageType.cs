using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Message type entity.</summary>
public class DbMessageType
{
    /// <summary>The id of a message type.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The name of a message type.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The alias of the message type.</summary>
    /// <remarks>Optional, but if set then the value must be Unique</remarks>
    public string? Alias { get; set; }
    /// <summary>The kind of the notification allowed .</summary>
    public MessageTypeClassification Classification { get; set; }
    /// <summary>The description of the message type.</summary> 
    public ICollection<DbRecipientCommunicationPreference> ContactPreferenceMessageTypes { get; set; } = new List<DbRecipientCommunicationPreference>();
}
