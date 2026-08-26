using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Payload of an <see cref="AgentsConstants.MediaTypes.Confirmation"/> content part: a two-way choice rendered as an
/// affirmative and a dismissive button. Picking one posts its label verbatim as the next user message, exactly as
/// <see cref="MultipleChoice"/> does — the label is the message.
/// </summary>
public class Confirmation
{
    /// <summary>Optional question shown above the buttons. Leave it null when the preceding prose already asks.</summary>
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    /// <summary>Label of the affirmative button, and the text posted when it is picked.</summary>
    [JsonPropertyName("confirmText")]
    public string ConfirmText { get; set; } = "Yes";

    /// <summary>Label of the dismissive button, and the text posted when it is picked.</summary>
    [JsonPropertyName("cancelText")]
    public string CancelText { get; set; } = "No";
}
