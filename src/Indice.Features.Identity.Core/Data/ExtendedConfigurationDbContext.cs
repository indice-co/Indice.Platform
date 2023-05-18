using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core.Data;

/// <summary>Seed options regarding <see cref="ExtendedConfigurationDbContext"/>.</summary>
public class ExtendedConfigurationDbContextSeedOptions
{
    /// <summary>A list of custom claim types to be inserted in the database on startup. Works only when environment is 'Development'.</summary>
    public IEnumerable<ClaimType> CustomClaims { get; set; } = new List<ClaimType>();
}
