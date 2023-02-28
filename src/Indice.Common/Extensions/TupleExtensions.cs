using System;
using System.Collections.Generic;
using System.Reflection;

namespace Indice.Extensions;

/// <summary>
/// Helper methods for working with <see cref="ValueTuple"/> struct.
/// </summary>
public static class TupleExtensions
{
    private static readonly HashSet<Type> ValueTupleTypes = new HashSet<Type>(new Type[] {
        typeof(ValueTuple<>),
        typeof(ValueTuple<,>),
        typeof(ValueTuple<,,>),
        typeof(ValueTuple<,,,>),
        typeof(ValueTuple<,,,,>),
        typeof(ValueTuple<,,,,,>),
        typeof(ValueTuple<,,,,,,>),
        typeof(ValueTuple<,,,,,,,>)
    });

    /// <summary>
    /// Checks if the provided object is a <see cref="ValueTuple"/>.
    /// </summary>
    /// <param name="object">The object to check.</param>
    public static bool IsValueTuple(this object @object) => IsValueTuple(@object.GetType());

    /// <summary>
    /// Checks if the provided object is a <see cref="ValueTuple"/>.
    /// </summary>
    /// <param name="type">The type to check.</param>
    public static bool IsValueTuple(this Type type) => type.GetTypeInfo().IsGenericType && ValueTupleTypes.Contains(type.GetGenericTypeDefinition());
}
