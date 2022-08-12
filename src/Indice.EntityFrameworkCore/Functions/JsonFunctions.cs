using System;
using Microsoft.EntityFrameworkCore.Query;

namespace Indice.EntityFrameworkCore.Functions
{
    // https://github.com/aspnet/EntityFrameworkCore/issues/11295#issuecomment-373852015
    /// <summary>Method stubs representing JSON path operations that can be translated to SQL statements by Entity Framework core.</summary>
    public static class JsonFunctions
    {
        /// <summary>Represents the json value expression on a given text column with inner path.</summary>
        /// <param name="column">The database column expression. In CLR terms the member parameter.</param>
        /// <param name="path">The JSON path.</param>
        /// <returns>The JSON value as string.</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static string JsonValue(object column, [NotParameterized] string path) {
            throw new NotSupportedException();
        }

        /// <summary>Database cast the input expression to <see cref="DateTime"/>.</summary>
        /// <param name="column"></param>
        /// <exception cref="NotSupportedException"></exception>
        public static DateTime? CastToDate(string column) {
            throw new NotSupportedException();
        }

        /// <summary>Database cast the input expression to <see cref="double" />.</summary>
        /// <param name="column"></param>
        /// <exception cref="NotSupportedException"></exception>
        public static double? CastToDouble(string column) {
            throw new NotSupportedException();
        }
    }
}
