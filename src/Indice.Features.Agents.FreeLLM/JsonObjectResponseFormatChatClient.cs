using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.FreeLLM;

/// <summary>
/// Adapts typed (JSON schema) requests for servers that do not support <c>response_format: json_schema</c>: it asks for a plain JSON object and
/// puts the schema in the instructions instead.
/// </summary>
/// <remarks>The agents framework sends a JSON schema for every typed reply (<c>RunAsync&lt;T&gt;</c>), so without this a server that rejects <c>json_schema</c> fails those steps.</remarks>
internal sealed class JsonObjectResponseFormatChatClient : DelegatingChatClient
{
    public JsonObjectResponseFormatChatClient(IChatClient innerClient) : base(innerClient) {
    }

    public override Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        base.GetResponseAsync(messages, Rewrite(options), cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        base.GetStreamingResponseAsync(messages, Rewrite(options), cancellationToken);

    /// <summary>Returns the options to send: a clone with a plain JSON response format when <paramref name="options"/> request a schema, otherwise <paramref name="options"/> itself.</summary>
    internal static ChatOptions? Rewrite(ChatOptions? options) {
        if (options?.ResponseFormat is not ChatResponseFormatJson { Schema: { } schema }) {
            return options;
        }
        // The agents share ChatOptions instances across calls, so never mutate the incoming ones.
        var rewritten = options.Clone();
        rewritten.ResponseFormat = ChatResponseFormat.Json;
        var instruction = $"Reply with a single JSON object only (no prose, no markdown fences) that conforms to this JSON schema:\n{schema.GetRawText()}";
        rewritten.Instructions = string.IsNullOrWhiteSpace(rewritten.Instructions) ? instruction : $"{rewritten.Instructions}\n\n{instruction}";
        return rewritten;
    }
}
