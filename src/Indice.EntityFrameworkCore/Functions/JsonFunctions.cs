using System;
using Microsoft.EntityFrameworkCore.Query;

namespace Indice.EntityFrameworkCore.Functions
{
    // https://github.com/aspnet/EntityFrameworkCore/issues/11295#issuecomment-373852015
    public static class JsonFunctions
    {
        public static string JsonValue(string column, [NotParameterized] string path) {
            throw new NotSupportedException();
        }

        public static DateTime? CastToDate(string column) {
            throw new NotSupportedException();
        }

        public static double? CastToDouble(string column) {
            throw new NotSupportedException();
        }
    }
}
