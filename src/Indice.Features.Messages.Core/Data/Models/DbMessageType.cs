namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Message type entity.</summary>
public class DbMessageType
{
    /// <summary>The id of a message type.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>The name of a message type.</summary>
    public string Name { get; set; }
    /// <summary>If true the type is used for commercial campaigns else it's for System.</summary>
    public bool IsCommercial { get; set; }
}
