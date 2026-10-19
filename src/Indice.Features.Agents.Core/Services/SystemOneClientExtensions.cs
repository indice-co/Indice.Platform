using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>Extension methods for <see cref="ISystemOneClient"/>.</summary>
public static class SystemOneClientExtensions
{
    /// <summary>Answers the <paramref name="options"/>' questions about the last of the <paramref name="messages"/>.</summary>
    /// <param name="client">The system-one client.</param>
    /// <param name="messages">The conversation, oldest first. The last message is the one the questions are about; the earlier ones are sent as history.</param>
    /// <param name="options">The questions to answer and the optional context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The answers, keyed by question name.</returns>
    /// <exception cref="ArgumentException">The last of the <paramref name="messages"/> has no text.</exception>
    /// <exception cref="HttpRequestException">The decision endpoint returned an unsuccessful status code.</exception>
    public static Task<SystemOneDecision> DecideAsync(this ISystemOneClient client, IEnumerable<ChatMessage> messages, SystemOneDecisionOptions options, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(client);
        return client.DecideAsync(SystemOneDecisionRequest.From(messages, options), cancellationToken);
    }
}
