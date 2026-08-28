using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Payload of a <see cref="AgentsConstants.MediaTypes.MultipleChoice"/> content part: a set of options the user can
/// pick from. The option string is both the label shown and the text posted — picking one sends it verbatim as the
/// next user message, exactly as if it had been typed.
/// </summary>
public class MultipleChoice
{
    /// <summary>The options offered to the user, in display order.</summary>
    [JsonPropertyName("options")]
    public List<string> Options { get; set; } = [];
}
