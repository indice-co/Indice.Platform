using System.Runtime.CompilerServices;
using System.Text;
using HandlebarsDotNet;

namespace Indice.Features.Messages.Core.Rendering;

/// <summary>
/// A text encoder that does not perform any encoding, used for Handlebars templates where the output is expected to be plain text and should not be HTML-encoded.
/// </summary>
public class HandlebarsPlainTextEncoder : ITextEncoder
{

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(StringBuilder? text, TextWriter target) {
        if (text != null && text.Length != 0) {
            target.Write(text.ToString());
        }
    }
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode(string? text, TextWriter target) {
        if (!string.IsNullOrEmpty(text)) {
            target.Write(text);
        }
    }
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Encode<T>(T? text, TextWriter target) where T : IEnumerator<char> {
        if (text != null) {
            while (text.MoveNext()) {
                target.Write(text.Current);
            }
        }
    }
}