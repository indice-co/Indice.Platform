using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Message type entity.</summary>
public class DbMessageType
{
    /// <summary>The id of a message type.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The name of a message type.</summary>
    public string Name { get; set; } = null!;

    /// <summary>The kind of the notification allowed .</summary>
    public MessageTypeClassification Classification { get; set; }
}
