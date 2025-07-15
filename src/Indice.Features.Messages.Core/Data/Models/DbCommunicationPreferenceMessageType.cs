using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Commnucation preferences per message type type.</summary>
public class DbCommunicationPreferenceMessageType
{
    /// <summary>The id of the distribution list.</summary>
    public Guid CommunicationPreferenceId { get; set; }
    /// <summary>Foreign key to the <see cref="DbMessageType"/>.</summary>
    public Guid TypeId { get; set; }
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public ContactChannelKind CommunicationPreferences { get; set; } = ContactChannelKind.Any;
    /// <summary>The type details of the campaign.</summary>
    public virtual DbMessageType MessageType { get; set; } = null!;
    /// <summary>The contact preference that this message type is associated with.</summary>
    public virtual DbCommunicationPreference CommunicationPreference { get; set; } = null!;
}
