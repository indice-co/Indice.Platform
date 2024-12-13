namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a campaign type.</summary>
public class MessageType
{
    /// <summary>The id of a campaign type.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of a campaign type.</summary>
    public string? Name { get; set; }
    /// <summary>The kind of the notification allowed .</summary>
    public MessageTypeClassification Classification { get; set; }
}

/// <summary>
/// How is the message classified
/// </summary>
public enum MessageTypeClassification : byte
{
    /// <summary>
    /// System notifications
    /// </summary>
    System = 0,
    /// <summary>
    /// Commercial notifications e.g. a campaign
    /// </summary>
    Commercial = 1
}