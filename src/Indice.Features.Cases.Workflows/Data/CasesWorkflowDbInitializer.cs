using Elsa.Persistence.EntityFramework.Core;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Workflows.Data;

/// <summary>Cases DbContext intialization extensions</summary>
public static class CasesDbInitalizerExtesnions
{
    /// <summary>
    /// Create database if not exists and seed with initial data
    /// </summary>
    /// <param name="dbContext">The database context</param>
    /// <param name="options">Seed options</param>
    /// <returns>The Task</returns>
    public async static Task InitializeAsync(this ElsaContext dbContext, IOptions<CasesWorkflowDbInitializerOptions> options) {
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
    public async static Task SeedAsync(this ElsaContext dbContext, IOptions<CasesWorkflowDbInitializerOptions> options) {
        var data = options.Value;
        await Task.CompletedTask;
    }

}
