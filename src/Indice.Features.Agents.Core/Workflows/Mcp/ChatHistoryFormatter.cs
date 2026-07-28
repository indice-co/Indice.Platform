using System.Text;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Mcp;

/// <summary>
/// Formats a list of <see cref="ChatMessage"/> history entries into a plain-text HISTORY block
/// suitable for inclusion in LLM prompts.
/// </summary>
public static class ChatHistoryFormatter
{
    /// <summary>
    /// Produces a multi-line HISTORY block from <paramref name="history"/>, oldest first.
    /// Returns an empty <see cref="StringBuilder"/> when <paramref name="history"/> is empty.
    /// </summary>
    public static StringBuilder Format(IReadOnlyList<ChatMessage> history) {
        var sb = new StringBuilder();
        foreach (var msg in history) {
            var role = msg.Role == ChatRole.User ? "User" : "Assistant";
            sb.AppendLine($"{role}: {msg.Text}");
        }
        return sb;
    }
}
