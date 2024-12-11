namespace Indice.Configuration;

/// <summary>Settings used for authentication of a client app.</summary>
public class ClientSettings
{
    /// <summary>The client id.</summary>
    public string? ClientId { get; set; }
    /// <summary>The client password.</summary>
    public string? ClientSecret { get; set; }
    /// <summary>The id of the tenant (directory).</summary>
    public string? TenantId { get; set; }
}
