using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Patch operation of a <see cref="DexChatStreamDelta"/> frame. RFC 6902 semantics plus the <c>append</c> protocol
/// extension (string append — the standard has none). v1 emits only <c>add</c> and <c>append</c>; <c>replace</c> is
/// reserved for future revision semantics.
/// </summary>
public enum DexChatPatchOp
{
    /// <summary>Adds an object member (replacing it when present, per RFC 6902) or appends an array element when the last path segment is <c>-</c>.</summary>
    [JsonStringEnumMemberName("add")]
    Add,
    /// <summary>Appends string <c>value</c> to the string at <c>path</c>. Protocol extension for token streaming.</summary>
    [JsonStringEnumMemberName("append")]
    Append,
    /// <summary>Replaces the value at <c>path</c>, which must exist. Reserved — not emitted in v1.</summary>
    [JsonStringEnumMemberName("replace")]
    Replace
}
