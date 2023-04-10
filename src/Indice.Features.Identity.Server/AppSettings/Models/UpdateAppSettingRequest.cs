using System.ComponentModel.DataAnnotations;

namespace Indice.Features.Identity.Server.AppSettings.Models;

/// <summary>Models an application setting that will be updated on the server.</summary>
public class UpdateAppSettingRequest
{
    /// <summary>The value of application setting.</summary>
    [Required]
    public string? Value { get; set; }
}
