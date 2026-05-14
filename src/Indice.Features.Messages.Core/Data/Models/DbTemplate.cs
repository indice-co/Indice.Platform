using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Template entity.</summary>
public class DbTemplate : DbAuditableEntity
{
    /// <summary>The unique id of the template.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of the template.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The alias of the message type.</summary>
    /// <remarks>Optional, but if set then the value must be Unique</remarks>
    public string? Alias { get; set; }
    /// <summary>Determines if the template to be created from this template should ignore user communication preferences.</summary>
    /// <remarks>This option can be overridden at campaign level</remarks>
    public bool IgnoreUserPreferences { get; set; }
    /// <summary>The contents of the template.</summary>
    public MessageContentDictionary Content { get; set; } = new();
    /// <summary>Sample data for the testing the template preview.</summary>
    /// <remarks>Optional</remarks>
    public dynamic? Data { get; set; }
    /// <summary>The id of a message type.</summary>
    public Guid? MessageTypeId { get; set; }
    /// <summary>The message type details of the template.</summary>
    public virtual DbMessageType? MessageType { get; set; }
    /// <summary>The type of the template.</summary>
    public TemplateType Type { get; set; }

}

/// <summary>
/// Represents the type of a template.
/// </summary>
public enum TemplateType : byte
{
    /// <summary>
    /// The template is a full template.
    /// </summary>
    Full = 0,
    /// <summary>
    /// The template is partial.
    /// </summary>
    Partial = 1,
    /// <summary>
    /// The template is a page layout / wrapper.
    /// </summary>
    Layout = 2
}

