using System.Globalization;
using System.Text;
using System.Text.Json;
using HandlebarsDotNet;

namespace Indice.Features.Messages.Core.Rendering;

/// <summary>
/// Registers the custom helpers available to message templates. The admin UI preview registers the same helpers
/// (<c>handlebars-helpers.ts</c>), so both must be kept in sync.
/// </summary>
public static class MessageHandlebarsHelpers
{
    /// <summary>
    /// Registers the message template helpers on the given <see cref="IHandlebars"/> environment.
    /// <list type="bullet">
    ///   <item><c>concat</c>: joins its arguments into a single string, e.g. <c>(concat data.baseUrl "/documents")</c>. Missing values render as empty.</item>
    /// </list>
    /// </summary>
    /// <param name="handlebars">The Handlebars environment.</param>
    /// <returns>The same environment, for chaining.</returns>
    public static IHandlebars RegisterMessageHelpers(this IHandlebars handlebars) {
        handlebars.RegisterHelper("concat", Concat);
        return handlebars;
    }

    // Returns a plain string: the caller encodes it once, whether it is printed directly or passed through a subexpression.
    private static object Concat(Context context, Arguments arguments) {
        var builder = new StringBuilder();
        for (var i = 0; i < arguments.Length; i++) {
            builder.Append(FormatArgument(arguments[i]));
        }
        return builder.ToString();
    }

    private static string FormatArgument(object? value) => value switch {
        // A missing path arrives as UndefinedBindingResult, whose ToString() is the property name.
        null or UndefinedBindingResult => string.Empty,
        string text => text,
        bool flag => flag ? "true" : "false",
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        JsonElement element => element.ValueKind switch {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => element.GetRawText()
        },
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };
}
