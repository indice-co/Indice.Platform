using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;

namespace Indice.Features.Cases.Tests;

internal static class CasesDbContextTestExtensions
{
    public static async Task SeedAsync(this CasesDbContext dbContext) {
        SeedInternal(dbContext);
        await dbContext.SaveChangesAsync();
    }

    public static void Seed(this CasesDbContext dbContext) {
        SeedInternal(dbContext);
        dbContext.SaveChanges();
    }

    private static void SeedInternal(CasesDbContext dbContext) {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] {
                new Claim("sub", "ab9769f1-d532-4b7d-9922-3da003157ebd"),
                new Claim("name", "tester"),
                new Claim("email", "tester@testakis.gr"),

            }, "test", "name", "role"));

        // put seed logic here
        var caseType = new DbCaseType {
            Id = Guid.NewGuid(),
            Title = "Applications",
            Code = "APPL",
            DataSchema = "{}"
        };
        dbContext.CaseTypes.Add(caseType);
        dbContext.Cases.Add(new DbCase {
            CaseTypeId = caseType.Id,
            Channel = "web",
            CreatedBy = AuditMeta.Create(principal),
            Versions = new List<DbCaseData> {
                new DbCaseData {
                    CreatedBy = AuditMeta.Create(principal),
                    Data = "{ \"test\": true }"
                }
            }
        });
    }
}
