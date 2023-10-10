using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Polly;

namespace Indice.Features.Identity.Core.Data;

internal static class ExtendedConfigurationDbContextExtensions
{
    public static void SeedInitialClaimTypes(this ExtendedConfigurationDbContext configurationDbContext) {
        var options = configurationDbContext.GetService<ExtendedConfigurationDbContextSeedOptions>() ?? new ExtendedConfigurationDbContextSeedOptions();
        var knownClaimNames = InitialClaimTypes.Get().Select(e => e.Name).ToArray();
        configurationDbContext.AddRange(InitialClaimTypes.Get());
        configurationDbContext.AddRange(options.CustomClaims.Where(x => !knownClaimNames.Contains(x.Name)));
        configurationDbContext.SaveChanges();
    }
}
