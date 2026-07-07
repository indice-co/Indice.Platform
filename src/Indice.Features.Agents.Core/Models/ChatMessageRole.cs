using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Author role of a chat message. Exposed at the service boundary and persisted per turn; serializes as its lowercase value (e.g. <c>user</c>).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ChatMessageRole>))]
public enum ChatMessageRole
{
    /// <summary>Message authored by the end user.</summary>
    [JsonStringEnumMemberName("user")]
    User,
    /// <summary>Message authored by the assistant.</summary>
    [JsonStringEnumMemberName("assistant")]
    Assistant,
    /// <summary>System / developer instruction message.</summary>
    [JsonStringEnumMemberName("system")]
    System,
    /// <summary>Tool invocation or tool result message.</summary>
    [JsonStringEnumMemberName("tool")]
    Tool
}
