using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;

namespace Indice.Features.Agents.FreeLLM;

/// <summary>
/// A pipeline policy that adapts <c>/chat/completions</c> request bodies to servers that are not strictly OpenAI: it renames
/// <c>max_completion_tokens</c> to <c>max_tokens</c> and can drop <c>stream_options</c>. Other requests pass through untouched.
/// </summary>
internal sealed class ChatCompletionsCompatibilityPolicy : PipelinePolicy
{
    private readonly bool _useMaxTokens;
    private readonly bool _streamUsage;

    public ChatCompletionsCompatibilityPolicy(bool useMaxTokens, bool streamUsage) {
        _useMaxTokens = useMaxTokens;
        _streamUsage = streamUsage;
    }

    /// <summary>Whether the options need this policy at all.</summary>
    public static bool IsNeeded(FreeLLMCompatibilityOptions options) => options.UseMaxTokens || !options.StreamUsage;

    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex) {
        var request = message.Request;
        if (IsChatCompletions(request) && request.Content is { } content) {
            using var buffer = new MemoryStream();
            content.WriteTo(buffer, message.CancellationToken);
            Apply(message, buffer);
        }
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex) {
        var request = message.Request;
        if (IsChatCompletions(request) && request.Content is { } content) {
            using var buffer = new MemoryStream();
            await content.WriteToAsync(buffer, message.CancellationToken).ConfigureAwait(false);
            Apply(message, buffer);
        }
        await ProcessNextAsync(message, pipeline, currentIndex).ConfigureAwait(false);
    }

    /// <summary>Rewrites a request body. Returns <c>null</c> when nothing needs to change.</summary>
    internal static string? Transform(string json, bool useMaxTokens, bool streamUsage) {
        if (JsonNode.Parse(json) is not JsonObject body) {
            return null;
        }
        var changed = false;
        if (useMaxTokens && body.Remove("max_completion_tokens", out var limit) && limit is not null) {
            body["max_tokens"] = limit;
            changed = true;
        }
        if (!streamUsage && body.Remove("stream_options")) {
            changed = true;
        }
        return changed ? body.ToJsonString() : null;
    }

    private void Apply(PipelineMessage message, MemoryStream buffer) {
        var json = System.Text.Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
        if (Transform(json, _useMaxTokens, _streamUsage) is { } rewritten) {
            message.Request.Content = BinaryContent.Create(BinaryData.FromString(rewritten));
        }
    }

    private static bool IsChatCompletions(PipelineRequest request) =>
        request.Uri?.AbsolutePath.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) == true;
}
