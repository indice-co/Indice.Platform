using System.Text;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>Formats conversation history into the plain-text block the LLM steps embed in their prompts.</summary>
internal static class ChatHistoryFormatter
{
    /// <summary>Renders the turns as one <c>role: content</c> line each (oldest-first); empty string when there is no history.</summary>
    public static string Format(IReadOnlyList<Models.ChatMessage> history) {
        if (history.Count == 0) {
            return string.Empty;
        }
        var sb = new StringBuilder();
        foreach (var message in history) {
            sb.Append(message.Role == Models.ChatRole.User ? "user: " : "assistant: ").AppendLine(message.Content);
        }
        return sb.ToString();
    }
}
