using System;
using System.Data;
using Indice.Hosting.Data;
using Npgsql;

namespace Indice.Hosting.Postgres
{
    /// <summary>
    /// An implementation of <see cref="IDbConnectionFactory"/> used to create database connections for SQL Server.
    /// </summary>
    public class PostgresConnectionFactory : IDbConnectionFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="PostgresConnectionFactory"/>.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public PostgresConnectionFactory(string connectionString) {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// The database connection string.
        /// </summary>
        public string ConnectionString { get; }

        /// <inheritdoc />
        public IDbConnection Create() => new NpgsqlConnection(ConnectionString);
    }
}
