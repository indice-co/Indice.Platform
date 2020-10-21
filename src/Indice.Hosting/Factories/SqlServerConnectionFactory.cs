using System.Data;
using System.Data.SqlClient;

namespace Indice.Hosting
{
    /// <summary>
    /// Provides an implementation of <see cref="IConnectionFactory"/> used to connect to a SQL Server database.
    /// </summary>
    public class SqlServerConnectionFactory : IConnectionFactory
    {
        public SqlServerConnectionFactory(string connectionString) {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        /// <inheritdoc />
        public IDbConnection Create() => new SqlConnection(ConnectionString);
    }
}
