using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Core.Data;

/// <summary>Cases DbContext intialization extensions</summary>
public static class CasesDbInitalizerExtesnions 
{
    /// <summary>
    /// Create database if not exists and seed with initial data
    /// </summary>
    /// <param name="dbContext">The database context</param>
    /// <param name="options">Seed options</param>
    /// <returns>The Task</returns>
    public async static Task InitializeAsync(this CasesDbContext dbContext, IOptions<CasesDbIntialDataOptions> options) {
        if (await dbContext.Database.EnsureCreatedAsync()) {
            await dbContext.SeedAsync(options);
        }
    }

    /// <summary>
    /// Seed the database to its initial state
    /// </summary>
    /// <param name="dbContext">The database context</param>
    /// <param name="options">Seed options</param>
    /// <returns></returns>
    public async static Task SeedAsync(this CasesDbContext dbContext, IOptions<CasesDbIntialDataOptions> options) {
        var data = options.Value;
        dbContext.CaseTypes.AddRange(data.CaseTypes);
        await dbContext.SaveChangesAsync();
    }
}
