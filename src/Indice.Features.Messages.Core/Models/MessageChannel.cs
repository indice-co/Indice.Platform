namespace Indice.Features.Messages.Core.Models;

/// <summary></summary>
public class MessageChannel
{
    /// <summary></summary>
    public string? Name { get; set; }
    /// <summary></summary>
    public MessageChannelKind Kind { get; set; }
    /// <summary></summary>
    public bool Enabled { get; set; }

}
