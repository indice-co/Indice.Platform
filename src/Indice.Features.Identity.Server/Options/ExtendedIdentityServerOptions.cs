using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Server.Options;

/// <inheritdoc/>
public class ExtendedIdentityServerOptions
{
    /// <summary>Configuration section name</summary>
    public const string Name = "IdentityServer";

    /// <summary>Additional endpoint options</summary>
    public IdentityServerEndpointOptions Endpoints { get; } = new IdentityServerEndpointOptions();
    /// <summary>Signing pfx file path. Relative to web content root</summary>
    public string SigningPfxFile { get; set; }
    /// <summary>Signing pfx pass</summary>
    public string SigningPfxPass { get; set; }
    /// <summary>ConfigurationDb connection string</summary>
    public string ConfigurationDbConnectionString { get; set; }
    /// <summary>OperationalDb connection string</summary>
    public string OperationalDbConnectionString { get; set; }
    /// <summary>IdentityDb connection string</summary>
    public string IdentityDbConnectionString { get; set; }
    /// <summary>If true, it seeds the database with some initial data for users. Works only when environment is 'Development'. Default is false.</summary>
    public bool SeedDummyUsers { get; set; } = false;
    /// <summary>If true, various events (user or client created etc.) are raised from the API. Default is false.</summary>
    public bool CanRaiseEvents { get; set; } = false;
    /// <summary>A list of initial users to be inserted in the database on startup. Works only when environment is 'Development'.</summary>
    public IEnumerable<User> InitialUsers { get; set; } = new List<User>();
    /// <summary>A list of custom claim types to be inserted in the database on startup. Works only when environment is 'Development'.</summary>
    public IEnumerable<ClaimType> CustomClaims { get; set; } = new List<ClaimType>();
    /// <summary>Disables the cache for all the endpoints in the IdentityServer API. Defaults to false.</summary>
    public bool DisableCache { get; set; } = false;
}
