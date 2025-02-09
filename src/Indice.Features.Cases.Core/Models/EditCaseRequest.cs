namespace Indice.Features.Cases.Core.Models;

/// <summary>The request payload with the edited data.</summary>
public class EditCaseRequest
{
    /// <summary>The data as json.</summary>
    public dynamic Data { get; set; } = null!;
}