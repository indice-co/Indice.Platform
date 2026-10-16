using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// A client for a system-one decision model (e.g. Microsoft-Decision-1), which answers typed, bounded questions about a
/// conversation with probabilities instead of generated text. The client only sends the request and parses the answers;
/// <see cref="SystemOneClientExtensions.DecideAsync(ISystemOneClient, IEnumerable{ChatMessage}, SystemOneDecisionOptions, CancellationToken)"/>
/// builds the request from chat messages, like <see cref="IChatClient"/>.
/// </summary>
public interface ISystemOneClient
{
    /// <summary>Answers the <paramref name="request"/>'s questions about its state.</summary>
    /// <param name="request">The state and the questions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The answers, keyed by question name.</returns>
    /// <exception cref="HttpRequestException">The decision endpoint returned an unsuccessful status code.</exception>
    Task<SystemOneDecision> DecideAsync(SystemOneDecisionRequest request, CancellationToken cancellationToken = default);
}
