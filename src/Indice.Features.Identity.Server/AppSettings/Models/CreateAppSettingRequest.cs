using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.AppSettings.Models;

/// <summary>Models an application setting persisted in the database.</summary>
public class CreateAppSettingRequest
{
    /// <summary>The key of application setting.</summary>
    [Required]
    public string? Key { get; set; }
    /// <summary>The value of application setting.</summary>
    [Required]
    public string? Value { get; set; }
}
