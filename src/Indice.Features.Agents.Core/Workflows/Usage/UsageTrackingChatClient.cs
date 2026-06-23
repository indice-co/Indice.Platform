using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Usage;

/// <summary>
/// A <see cref="DelegatingChatClient"/> that records the token usage of every response into a
/// request-scoped <see cref="TokenUsageAccumulator"/>. Wrapped around the reasoning chat client only
/// (via the <c>clientFactory</c> hook on <c>AsAIAgent</c>), so only reasoning-model usage is counted.
/// It never alters the response — it observes <see cref="ChatResponse.Usage"/> on the non-streaming path
/// (intent classification) and the trailing <see cref="UsageContent"/> on the streaming path (answer
/// composition).
/// </summary>
public sealed class UsageTrackingChatClient : DelegatingChatClient
{
    private readonly TokenUsageAccumulator _accumulator;
    private readonly string _model;

    /// <summary>Creates a new <see cref="UsageTrackingChatClient"/> wrapping <paramref name="innerClient"/>.</summary>
    public UsageTrackingChatClient(IChatClient innerClient, TokenUsageAccumulator accumulator, string model) : base(innerClient) {
        _accumulator = accumulator;
        _model = model;
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) {
        // The non-streaming path: AIAgent.RunAsync / RunAsync<T> (used by IntentClassifier).
        var response = await base.GetResponseAsync(messages, options, cancellationToken);
        _accumulator.Add(response.Usage, _model);
        return response;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        // The streaming path: AIAgent.RunStreamingAsync (used by AnswerComposer). Usage arrives as a
        // trailing UsageContent on the final update(s); accumulate it without altering the stream.
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken)) {
            foreach (var content in update.Contents) {
                if (content is UsageContent usage) {
                    _accumulator.Add(usage.Details, _model);
                }
            }
            yield return update;
        }
    }
}
