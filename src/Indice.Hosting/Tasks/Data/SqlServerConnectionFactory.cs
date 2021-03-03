using System;
using System.Data;
using System.Data.SqlClient;

namespace Indice.Hosting.Tasks.Data
{
    /// <summary>
    /// A factory class that generates instances of type <see cref="IDbConnection"/>.
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Constructs a new instance of type <see cref="IDbConnection"/>.
        /// </summary>
        IDbConnection Create();
    }

    /// <summary>
    /// Helper methods for <see cref="IDbCommand"/>.
    /// </summary>
    public static class IDbCommandExtensions
    {
        /// <summary>
        /// Adds a new parameter to a SQL statement with the specified name and value.
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="parameterValue">The parameter value.</param>
        /// <param name="paramaterType"></param>
        public static void AddParameterWithValue(this IDbCommand command, string parameterName, object parameterValue, DbType paramaterType) {
            var parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            parameter.Value = parameterValue;
            parameter.DbType = paramaterType;
            command.Parameters.Add(parameter);
        }
    }

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
