using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Commnucation preferences per message type type.</summary>
public class DbContactCommunicationOption
{
    /// <summary>The id of the related <see cref="DbContactPreference"/>.</summary>
    public Guid ContactPreferenceId { get; set; }
    /// <summary>Foreign key to the <see cref="DbMessageType"/>.</summary>
    public Guid MessageTypeId { get; set; }
    /// <summary>The preferred delivery channels to receive messages.</summary>
    public ContactChannelKind Channels { get; set; } = ContactChannelKind.Any;
    /// <summary>The message type</summary>
    public virtual DbMessageType MessageType { get; set; } = null!;
    /// <summary>The contact preference that this message type is associated with.</summary>
    public virtual DbContactPreference ContactPreference { get; set; } = null!; 
    /// <summary>Indicates when record was last updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

}
