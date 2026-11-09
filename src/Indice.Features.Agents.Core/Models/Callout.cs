using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Payload of an <see cref="AgentsConstants.MediaTypes.Callout"/> content part: a short notice the chat UI renders as a
/// highlighted alert rather than as prose — a disclaimer, a policy warning, or a caveat about the answer's completeness.
/// </summary>
public class Callout
{
    /// <summary>The severities the chat UI knows how to render. Anything else falls back to <see cref="Info"/>.</summary>
    public static class Severities
    {
        /// <summary>Neutral information.</summary>
        public const string Info = "info";

        /// <summary>A positive outcome or confirmation.</summary>
        public const string Success = "success";

        /// <summary>Something the reader should be careful about.</summary>
        public const string Warning = "warning";

        /// <summary>Something that went wrong or must not be done.</summary>
        public const string Error = "error";
    }

    /// <summary>
    /// How prominently to render the notice — one of the <see cref="Severities"/> values. Deliberately a string and not
    /// an enum: an older client meeting a severity it has never seen falls back to <see cref="Severities.Info"/> instead
    /// of failing to deserialize the part.
    /// </summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = Severities.Info;

    /// <summary>Optional bold heading shown above the body.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>The body of the notice. Rendered as plain text — line breaks are preserved, markdown is not interpreted.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
