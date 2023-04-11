namespace Indice.Features.Identity.Server.AppSettings.Models;

/// <summary>Models an application setting persisted in the database.</summary>
public class AppSettingInfo
{
    /// <summary>The key of application setting.</summary>
    public string? Key { get; set; }
    /// <summary>The value of application setting.</summary>
    public string? Value { get; set; }
}
