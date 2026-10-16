using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// A client for a system-one decision model (e.g. Microsoft-Decision-1), which answers typed, bounded questions about a
/// conversation with probabilities instead of generated text. Shaped like <see cref="IChatClient"/>: the caller supplies
/// the messages and the options, the client only builds, sends and parses the request.
/// </summary>
public interface ISystemOneClient
{
    /// <summary>Answers the <paramref name="options"/>' questions about the last of the <paramref name="messages"/>.</summary>
    /// <param name="messages">The conversation, oldest first. The last message is the one the questions are about; the earlier ones are sent as history.</param>
    /// <param name="options">The questions to answer and the optional context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The answers, keyed by question name.</returns>
    /// <exception cref="HttpRequestException">The decision endpoint returned an unsuccessful status code.</exception>
    Task<SystemOneDecision> DecideAsync(IEnumerable<ChatMessage> messages, SystemOneDecisionOptions options, CancellationToken cancellationToken = default);
}
