using Indice.Features.Cases.Data;

namespace Indice.Cases.Data
{
    internal static class CasesDbContextSeed
    {
        public static void SeedCasesDatabase(this IApplicationBuilder app) {
            using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()?.CreateScope();
            var context = serviceScope?.ServiceProvider.GetRequiredService<CasesDbContext>();
            if (context != null) {
                SeedCaseTypes(context);
            }
        }

        private static void SeedCaseTypes(CasesDbContext context) {
            if (context.CaseTypes.Any()) {
                // Seed only when database is empty
                return;
            }

            context.SaveChanges();
        }
    }
}
