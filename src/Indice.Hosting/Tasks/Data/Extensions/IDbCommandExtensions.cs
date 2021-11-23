using System.Data;

namespace Indice.Hosting.Data
{
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
}
