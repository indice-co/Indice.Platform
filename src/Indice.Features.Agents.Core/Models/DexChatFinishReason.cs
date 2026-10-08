using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Specifies the reason why the chat completion model stopped generating tokens.
/// </summary>
public enum DexChatFinishReason
{
    /// <summary>
    /// The model hit a natural stop point or a provided stop sequence.
    /// </summary>
    [JsonStringEnumMemberName("stop")]
    Stop = 0,

    /// <summary>
    /// The maximum number of tokens specified in the request was reached.
    /// </summary>
    [JsonStringEnumMemberName("length")]
    Length = 1,

    /// <summary>
    /// The model called a tool.
    /// </summary>
    [JsonStringEnumMemberName("tool_calls")]
    ToolCalls = 2,

    /// <summary>
    /// Content was omitted due to a flag from the content filters.
    /// </summary>
    [JsonStringEnumMemberName("content_filter")]
    ContentFilter = 3,

    /// <summary>
    /// The completion token limit was reached.
    /// </summary>
    [JsonStringEnumMemberName("limit")]
    Limit = -1
}
