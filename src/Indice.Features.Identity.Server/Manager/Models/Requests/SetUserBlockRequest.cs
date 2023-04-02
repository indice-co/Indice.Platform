namespace Indice.Features.Identity.Server.Manager.Models;

/// <summary>Models toggling a user's 'Blocked' property.</summary>
public class SetUserBlockRequest
{
    /// <summary>Indicates whether the user is forcefully blocked.</summary>
    public bool Blocked { get; set; }
}
