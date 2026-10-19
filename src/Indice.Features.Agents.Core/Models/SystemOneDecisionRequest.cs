using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>A request to a system-one decision model: the state to judge and the typed questions to answer about it.</summary>
public class SystemOneDecisionRequest
{
    /// <summary>The decision model deployment, e.g. <c>Decision-1</c>. <c>null</c> uses the client's deployment.</summary>
    public string? Model { get; init; }

    /// <summary>What the questions are about.</summary>
    public SystemOneState State { get; init; } = new();

    /// <summary>The questions to answer, keyed by the name the answers come back under.</summary>
    public Dictionary<string, SystemOneQuestion> Questions { get; init; } = [];

    /// <summary>
    /// Creates a request about the last of the <paramref name="messages"/>; the earlier ones become the history and the
    /// <paramref name="options"/>' instructions the context. Earlier messages without text are skipped.
    /// </summary>
    /// <param name="messages">The conversation, oldest first.</param>
    /// <param name="options">The questions to answer and the optional context.</param>
    /// <exception cref="ArgumentException">The last of the <paramref name="messages"/> has no text.</exception>
    public static SystemOneDecisionRequest From(IEnumerable<ChatMessage> messages, SystemOneDecisionOptions options) {
        ArgumentNullException.ThrowIfNull(messages);
        ArgumentNullException.ThrowIfNull(options);
        var input = messages.ToList();
        if (input.Count == 0 || string.IsNullOrWhiteSpace(input[^1].Text)) {
            throw new ArgumentException("The last message must contain text.", nameof(messages));
        }
        var turns = input.Where(message => !string.IsNullOrWhiteSpace(message.Text)).ToList();
        return new SystemOneDecisionRequest {
            State = new SystemOneState {
                Context = string.IsNullOrWhiteSpace(options.Instructions) ? null : options.Instructions.Trim(),
                History = turns.Count > 1
                    ? [.. turns.Take(turns.Count - 1).Select(turn => new SystemOneTurn { Role = turn.Role.Value, Text = turn.Text })]
                    : null,
                Message = turns[^1].Text,
            },
            Questions = options.Questions,
        };
    }
}

/// <summary>The state a <see cref="SystemOneDecisionRequest"/> asks about: the latest message, the history before it and the context.</summary>
public class SystemOneState
{
    /// <summary>Background the model judges the message against; optional.</summary>
    public string? Context { get; init; }

    /// <summary>The earlier turns of the conversation, oldest first; optional.</summary>
    public List<SystemOneTurn>? History { get; init; }

    /// <summary>The message the questions are about.</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>One earlier turn of the conversation in a <see cref="SystemOneState"/>.</summary>
public class SystemOneTurn
{
    /// <summary>Who wrote it, e.g. <c>user</c> or <c>assistant</c>.</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>What was written.</summary>
    public string Text { get; init; } = string.Empty;
}
