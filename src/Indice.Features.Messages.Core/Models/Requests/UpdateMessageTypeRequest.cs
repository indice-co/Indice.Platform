namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to update a campaign type.</summary>
public class UpdateMessageTypeRequest
{
    /// <summary>The name of a campaign type.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The Alias of the campaign type.</summary>
    public string? Alias { get; set; }
    /// <summary>The Classification of the campaign type.</summary>
    public MessageTypeClassification Classification { get; set; }
}
