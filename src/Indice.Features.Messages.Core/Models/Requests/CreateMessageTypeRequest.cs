namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to create a new campaign type.</summary>
public class CreateMessageTypeRequest
{
    /// <summary>The name of a campaign type.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The alias of the campaign type.</summary>
    public string? Alias { get; set; }
    /// <summary>The Classification of the campaign type.</summary>
    public MessageTypeClassification Classification { get; set; }
}
