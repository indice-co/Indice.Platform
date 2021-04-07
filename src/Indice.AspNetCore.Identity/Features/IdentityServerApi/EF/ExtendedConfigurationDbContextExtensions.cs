using System;
using System.Collections.Generic;
using System.Linq;

namespace Indice.AspNetCore.Identity.Features
{
    internal static class ExtendedConfigurationDbContextExtensions
    {
        public static void SeedCustomClaimTypes(this ExtendedConfigurationDbContext configurationDbContext, IEnumerable<ClaimType> claimTypes) {
            var knownClaimNames = InitialClaimTypes.Get().Select(e => e.Name).ToArray();
            configurationDbContext.AddRange(claimTypes.Where(x => !knownClaimNames.Contains(x.Name)));
        }

        public static void SeedInitialClaimTypes(this ExtendedConfigurationDbContext configurationDbContext) {
            configurationDbContext.AddRange(InitialClaimTypes.Get());
        }
    }
}
