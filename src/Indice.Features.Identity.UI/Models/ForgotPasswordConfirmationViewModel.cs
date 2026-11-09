namespace Indice.Features.Identity.UI.Models;

/// <summary>View model for forgot password confirmation model.</summary>
public class ForgotPasswordConfirmationViewModel
{
    /// <summary>The user identifier.</summary>
    public string? UserId { get; set; }
    /// <summary>The user name.</summary>
    public string? UserName { get; set; }
    /// <summary>Specifies whether a device (browser) id should be generated.</summary>
    public bool GenerateDeviceId { get; set; } = true;
}
