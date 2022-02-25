using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore
{
    internal static class DatabaseFacadeExtensions
    {
        public static async Task<DbConnection> EnsureOpenConnectionAsync(this DatabaseFacade database) {
            // We do not need to add the call to GetDbConnection in a using statement.
            // When injecting DbContext through DI, then DI will close the underlying connection when DbContext is disposed.
            // We also need to ensure that connection is not opened multiple times.
            var dbConnection = database.GetDbConnection();
            if (dbConnection.State == ConnectionState.Closed) {
                await dbConnection.OpenAsync();
            }
            return dbConnection;
        }
    }
}
