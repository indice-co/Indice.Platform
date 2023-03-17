namespace Indice.AspNetCore.Features.Settings.Models;

/// <summary>Models an application setting that will be updated on the server.</summary>
public class UpdateAppSettingRequest
{
    /// <summary>The value of application setting.</summary>
    public string Value { get; set; }
}
