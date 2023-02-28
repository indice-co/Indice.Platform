using System;
using System.Text.Json;

namespace Indice.Extensions;

/// <summary>
/// Extension methods related to JSON serialization.
/// </summary>
public static partial class JsonExtensions
{
    /// <summary>
    /// Converts a <see cref="JsonElement"/> to the given type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert.</typeparam>
    /// <param name="element">A specific value of the JSON content.</param>
    /// <param name="options">Provides options to be used with <see cref="JsonSerializer"/>.</param>
    public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null) {
        var json = element.GetRawText();
        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to the given type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert.</typeparam>
    /// <param name="document">The JSON content.</param>
    /// <param name="options">Provides options to be used with <see cref="JsonSerializer"/>.</param>
    public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions options = null) =>
        ToObject<T>(document.RootElement, options);

    /// <summary>
    /// Converts a <see cref="JsonElement"/> to the given type <paramref name="returnType"/>.
    /// </summary>
    /// <param name="element">A specific value of the JSON content.</param>
    /// <param name="returnType">The type to return.</param>
    /// <param name="options">Provides options to be used with <see cref="JsonSerializer"/>.</param>
    public static object ToObject(this JsonElement element, Type returnType, JsonSerializerOptions options = null) {
        var json = element.GetRawText();
        return JsonSerializer.Deserialize(json, returnType, options);
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> to the given type <paramref name="returnType"/>.
    /// </summary>
    /// <param name="document">The JSON content.</param>
    /// <param name="returnType">The type to return.</param>
    /// <param name="options">Provides options to be used with <see cref="JsonSerializer"/>.</param>
    public static object ToObject(this JsonDocument document, Type returnType, JsonSerializerOptions options = null) =>
        ToObject(document.RootElement, returnType, options);
}
