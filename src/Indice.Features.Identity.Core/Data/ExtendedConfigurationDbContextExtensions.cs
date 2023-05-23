using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Indice.Features.Identity.Core.Data;

internal static class ExtendedConfigurationDbContextExtensions
{
    public static void SeedCustomClaimTypes(this ExtendedConfigurationDbContext configurationDbContext, IEnumerable<ClaimType> claimTypes) {
        var knownClaimNames = InitialClaimTypes.Get().Select(e => e.Name).ToArray();
        configurationDbContext.AddRange(claimTypes.Where(x => !knownClaimNames.Contains(x.Name)));
    }

    public static void SeedInitialClaimTypes(this ExtendedConfigurationDbContext configurationDbContext) {
        var options = configurationDbContext.GetService<ExtendedConfigurationDbContextSeedOptions>() ?? new ExtendedConfigurationDbContextSeedOptions();
        configurationDbContext.AddRange(InitialClaimTypes.Get());
        configurationDbContext.AddRange(options.CustomClaims);
    }
}
