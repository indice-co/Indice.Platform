using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Identity.Server.Options;
/// <summary>The <see cref="ExtendedIdentityServerOptions"/> is the top level container for all configuration settings of Identity Server.</summary>
public class ExtendedIdentityServerOptions
{
    /// <summary>Configuration section name</summary>
    public const string Name = "IdentityServer";
    /// <summary>Signing .pfx file path. Relative to web content root.</summary>
    public string? SigningPfxFile { get; set; }
    /// <summary>Signing .pfx password.</summary>
    public string? SigningPfxPass { get; set; }
    /// <summary>Callback to configure the Identity DbContext. If not set then SQL Server is selected by default using <strong>IdentityDb</strong> as connection string name.</summary>
    public Action<DbContextOptionsBuilder>? ConfigureIdentityDbContext { get; set; }
    /// <summary>Callback to configure the DbContext for configuration store. If not set then SQL Server is selected by default using <strong>ConfigurationDb</strong> as connection string name.</summary>
    public Action<DbContextOptionsBuilder>? ConfigureConfigurationDbContext { get; set; }
    /// <summary>Callback to configure the DbContext for persisted grants. If not set then SQL Server is selected by default using <strong>OperationalDb</strong> as connection string name.</summary>
    public Action<DbContextOptionsBuilder>? ConfigurePersistedGrantDbContext { get; set; }
    /// <summary>If true, it seeds the database with some initial data for users. Works only when environment is 'Development'. Default is false.</summary>
    public bool SeedDummyUsers { get; set; } = false;
    /// <summary>If true, various events (user or client created etc.) are raised from the API. Default is false.</summary>
    public bool CanRaiseEvents { get; set; } = false;
    /// <summary>A list of initial users to be inserted in the database on startup. Works only when environment is 'Development'.</summary>
    public IEnumerable<User> InitialUsers { get; set; } = new List<User>();
    /// <summary>A list of custom roles to add to the identity server database</summary>
    public IEnumerable<Role> CustomRoles { get; set; } = new List<Role>();
    /// <summary>A list of custom claim types to be inserted in the database on startup. Works only when environment is 'Development'.</summary>
    public IEnumerable<ClaimType> CustomClaims { get; set; } = new List<ClaimType>();
}
