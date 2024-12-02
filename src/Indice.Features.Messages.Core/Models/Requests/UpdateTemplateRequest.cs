namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to update an existing template.</summary>
public class UpdateTemplateRequest
{
    /// <summary>The name of the template.</summary>
    public string Name { get; set; }
    /// <summary>Determines if the taemplate to be created from this template should ignore user communication preferences.</summary>
    public bool IgnoreUserPreferences { get; set; }
    /// <summary>The content of the template.</summary>
    public MessageContentDictionary Content { get; set; } = new();
    /// <summary>Sample data for the template</summary>
    public dynamic Data { get; set; }
}
