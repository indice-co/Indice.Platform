using Indice.Features.Cases.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Extensions;

internal static class CasesDbContextExtensions
{
    /// <summary>
    /// Gets the next value of the `ReferenceNumberSequence` sequence.
    /// </summary>
    public static async Task<int> GetNextReferenceNumber(this CasesDbContext dbContext) {
        var result = new SqlParameter("@result", System.Data.SqlDbType.Int) {
            Direction = System.Data.ParameterDirection.Output
        };
        await dbContext.Database.ExecuteSqlRawAsync($"SET @result = NEXT VALUE FOR [{CasesApiConstants.DatabaseSchema}].[{CasesApiConstants.ReferenceNumberSequence}]", result);

        var value = (int)result.Value;
        return value;
    }
}
