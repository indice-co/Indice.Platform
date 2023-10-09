namespace Indice.Features.Media.AspNetCore.Models.Requests;
/// <summary>The request model used to update an existing setting.</summary>
public class UpdateMediaSettingRequest

{
    /// <summary>The setting's value.</summary>
    public required string Value { get; set; }
}
