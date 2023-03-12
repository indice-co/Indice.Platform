namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to create a new template.</summary>
public class CreateTemplateRequest
{
    /// <summary>The name of the template.</summary>
    public string Name { get; set; }
    /// <summary>The content of the template.</summary>
    public MessageContentDictionary Content { get; set; } = new MessageContentDictionary();
}
