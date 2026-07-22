using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>Author role of a chat message at the API boundary. Mirrors the well-known <c>Microsoft.Extensions.AI.ChatRole</c> values 1-1.</summary>
public enum DexChatRole
{
    /// <summary>The message was authored by the end user.</summary>
    [JsonStringEnumMemberName("user")]
    User = 0,

    /// <summary>The message was authored by the assistant.</summary>
    [JsonStringEnumMemberName("assistant")]
    Assistant = 1,

    /// <summary>The message is a system instruction.</summary>
    [JsonStringEnumMemberName("system")]
    System = 2,

    /// <summary>The message carries a tool result.</summary>
    [JsonStringEnumMemberName("tool")]
    Tool = 3
}
