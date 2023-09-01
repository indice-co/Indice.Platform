namespace Indice.Features.Identity.Core.Data;

/// <summary>Seed options regarding <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</summary>
public class ExtendedIdentityDbContextSeedOptions<TUser, TRole>
{
    /// <summary>A list of initial users to be inserted in the database on startup. Works only when environment is 'Development'.</summary>
    public IEnumerable<TUser> InitialUsers { get; set; } = new List<TUser>();
    /// <summary>A list of initial users to be inserted in the database on startup. Works only when environment is 'Development'.</summary>
    public IEnumerable<TRole> CustomRoles { get; set; } = new List<TRole>();
}
