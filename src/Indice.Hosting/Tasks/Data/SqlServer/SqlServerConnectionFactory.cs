using System;
using System.Data;
using System.Data.SqlClient;
using Indice.Hosting.Data;

namespace Indice.Hosting.SqlServer
{
    /// <summary>
    /// An implementation of <see cref="IDbConnectionFactory"/> used to create database connections for SQL Server.
    /// </summary>
    public class SqlServerConnectionFactory : IDbConnectionFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="SqlServerConnectionFactory"/>.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public SqlServerConnectionFactory(string connectionString) {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// The database connection string.
        /// </summary>
        public string ConnectionString { get; }

        /// <inheritdoc />
        public IDbConnection Create() => new SqlConnection(ConnectionString);
    }
}
