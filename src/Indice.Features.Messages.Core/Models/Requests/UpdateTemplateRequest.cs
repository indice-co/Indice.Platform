namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to update an existing template.</summary>
public class UpdateTemplateRequest
{
    /// <summary>The name of the template.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The content of the template.</summary>
    public MessageContentDictionary Content { get; set; } = new();
    /// <summary>Sample data for the template</summary>
    public dynamic? Data { get; set; }
}
