using System;
using System.Collections.Generic;

namespace Indice.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="Enum"/> class.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the values of a flags enum as an enumerable.
        /// </summary>
        /// <typeparam name="T">The flags enum.</typeparam>
        /// <param name="flagsEnum">The type of enum.</param>
        public static IEnumerable<T> GetValues<T>(this T flagsEnum) where T : Enum {
            foreach (Enum value in Enum.GetValues(flagsEnum.GetType())) {
                if (flagsEnum.HasFlag(value)) {
                    yield return (T)value;
                }
            }
        }
    }
}
