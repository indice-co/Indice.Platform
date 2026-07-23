using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Tests.Streaming;

/// <summary>
/// Reference applier for the Dex streaming patch protocol: RFC 6901 pointer resolution, ops add/append/replace, and
/// frame-compaction inflation (an omitted <c>path</c>/<c>op</c> inherits the previous delta's effective value — one
/// instance per stream). Executable spec and port target for the TypeScript client.
/// </summary>
internal sealed class JsonPointerPatch
{
    private string? _path;
    private DexChatPatchOp? _op;

    public void Apply(JsonObject document, DexChatStreamDelta frame, JsonSerializerOptions options) {
        _path = frame.Path ?? _path ?? throw new InvalidOperationException("First delta frame carries no path.");
        _op = frame.Op ?? _op ?? throw new InvalidOperationException("First delta frame carries no op.");
        var value = frame.Value is null ? null : JsonSerializer.SerializeToNode(frame.Value, options);
        var segments = _path.TrimStart('/').Split('/').Select(Unescape).ToArray();
        var parent = Resolve(document, segments[..^1]);
        var last = segments[^1];
        switch (_op) {
            case DexChatPatchOp.Add:
                if (parent is JsonArray array) {
                    if (last == "-") { array.Add(value); } else { array.Insert(int.Parse(last), value); }
                } else {
                    ((JsonObject)parent)[last] = value;
                }
                break;
            case DexChatPatchOp.Append:
                var target = (JsonObject)parent;
                var existing = target[last]?.GetValue<string>() ?? string.Empty;
                target[last] = existing + value!.GetValue<string>();
                break;
            case DexChatPatchOp.Replace:
                var obj = (JsonObject)parent;
                if (!obj.ContainsKey(last)) { throw new InvalidOperationException($"replace target '{_path}' does not exist."); }
                obj[last] = value;
                break;
            default:
                throw new InvalidOperationException($"Unsupported op '{_op}'.");
        }
    }

    private static JsonNode Resolve(JsonNode root, string[] segments) {
        var node = root;
        foreach (var segment in segments) {
            node = node is JsonArray array ? array[int.Parse(segment)]! : node![segment]!;
        }
        return node;
    }

    private static string Unescape(string segment) => segment.Replace("~1", "/").Replace("~0", "~");
}
