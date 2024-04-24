namespace Indice.Features.Messages.Core.Models;

/// <summary>Models a template.</summary>
public class Template : TemplateBase
{
    /// <summary>The content of the template.</summary>
    public MessageContentDictionary Content { get; set; } = [];
}

/// <summary>Models a template when retrieved on a list.</summary>
public class TemplateListItem : TemplateBase
{
    /// <summary>The channels that this template supports.</summary>
    public MessageChannelKind Channels { get; set; }
}

/// <summary>Models a template's basic information.</summary>
public class TemplateBase
{
    /// <summary>The unique id of the template.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of the template.</summary>
    public string Name { get; set; }
    /// <summary>Specifies the principal that created the template.</summary>
    public string CreatedBy { get; set; }
    /// <summary>Specifies when a template was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Specifies the principal that update the template.</summary>
    public string UpdatedBy { get; set; }
    /// <summary>Specifies when a template was updated.</summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
