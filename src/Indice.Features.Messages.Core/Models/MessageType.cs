namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a campaign type.</summary>
public class MessageType
{
    /// <summary>The id of a campaign type.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of a campaign type.</summary>
    public string? Name { get; set; }
    /// <summary>The alias of a campaign type.</summary>
    public string? Alias { get; set; }
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
    /// Commercial campaigns
    /// </summary>
    Commercial = 1,
    /// <summary>
    /// Informational
    /// </summary>
    Info = 100,
    /// <summary>
    /// Successful operations
    /// </summary>
    Success = 101,
    /// <summary>
    /// Error notifications
    /// </summary>
    Error = 102,
    /// <summary>
    /// Warnings
    /// </summary>
    Warning = 103,
}