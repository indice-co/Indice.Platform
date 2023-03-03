using System;
using System.Collections.Generic;

namespace Indice.Extensions;

/// <summary>
/// Extension methods on <see cref="Enum"/> class.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the values of a flags enum as an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The flags enum.</typeparam>
    /// <param name="flagsEnum">The type of enum.</param>
    public static IEnumerable<T> GetFlagValues<T>(this T flagsEnum) where T : Enum {
        var values = Enum.GetValues(flagsEnum.GetType());
        foreach (Enum value in values) {
            var isDefaultValue = Enum.ToObject(typeof(T), 0).ToString() == value.ToString();
            if (!isDefaultValue && flagsEnum.HasFlag(value)) {
                yield return (T)value;
            }
        }
    }
}
