namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a campaign type.</summary>
public class MessageType
{
    /// <summary>The id of a campaign type.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of a campaign type.</summary>
    public string Name { get; set; }

    /// <summary>If true the type is used for commercial campaigns else it's for System.</summary>
    public bool Commercial { get; set; }
}
