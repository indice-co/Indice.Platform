namespace Indice.Features.Identity.Server.Manager.Models;
/// <summary>Grant info dto</summary>
public class UserGrantInfo
{
    /// <summary>The session id</summary>
    public string? SessionId { get; set; }
    /// <summary>Grant type.</summary>
    /// <remarks>Can be one of user_consent, authorization_code, reference_token, refresh_token</remarks>
    public string Type { get; set; } = null!;
    /// <summary>Grant creation <see cref="DateTime"/>.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Grant expiration <see cref="DateTime"/>.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>The jti token identifier claim for the accss token.</summary>
    public string? TokenId { get; set; }
    /// <summary>The device id that created the grant.</summary>
    public string? DeviceId { get; set; }
    /// <summary>The IP address of the user that created the grant.</summary>
    public string? IpAddress { get; set; }
}
