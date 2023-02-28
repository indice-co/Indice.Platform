using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.Core.Data;

internal static class ExtendedConfigurationDbContextExtensions
{
    public static void SeedCustomClaimTypes(this ExtendedConfigurationDbContext configurationDbContext, IEnumerable<DbClaimType> claimTypes) {
        var knownClaimNames = InitialClaimTypes.Get().Select(e => e.Name).ToArray();
        configurationDbContext.AddRange(claimTypes.Where(x => !knownClaimNames.Contains(x.Name)));
    }

    public static void SeedInitialClaimTypes(this ExtendedConfigurationDbContext configurationDbContext) {
        configurationDbContext.AddRange(InitialClaimTypes.Get());
    }
}
