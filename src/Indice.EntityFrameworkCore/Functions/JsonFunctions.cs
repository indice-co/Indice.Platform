using System;
using Microsoft.EntityFrameworkCore.Query;

namespace Indice.EntityFrameworkCore.Functions
{
    // https://github.com/aspnet/EntityFrameworkCore/issues/11295#issuecomment-373852015
    /// <summary>
    /// Method stubs representing Json path operations that can be translated to SQL statements by EntityFramewrok core.
    /// </summary>
    public static class JsonFunctions
    {
        /// <summary>
        /// Represents the json value expression on a given text column with inner path
        /// </summary>
        /// <param name="column">The database column expression. In clr terms the member parameter</param>
        /// <param name="path">The json path</param>
        /// <returns>The json value as string</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static string JsonValue(object column, [NotParameterized] string path) {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Database Cast the input expression to <see cref="DateTime"/>
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static DateTime? CastToDate(string column) {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Database Cast the input expression to <see cref="double" />.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static double? CastToDouble(string column) {
            throw new NotSupportedException();
        }
    }
}
