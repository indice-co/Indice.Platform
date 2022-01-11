using System;
using System.Linq;

namespace Indice.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="Type"/>.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Determines whether a type is decorated with <see cref="FlagsAttribute"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        public static bool IsFlagsEnum(this Type type) => 
            (type.IsEnum && type.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Any()) || 
            (Nullable.GetUnderlyingType(type)?.IsEnum == true && Nullable.GetUnderlyingType(type).GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Any());
    }
}
