using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Tests.Streaming;

public class DexChatStreamFrameSerializationTests
{
    // Mirrors the host pipeline: web defaults (camelCase) + string enums, same as the SSE writer sees.
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerOptions.Web) {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void Delta_frame_serializes_type_op_path_value() {
        DexChatResponseUpdate frame = new DexChatStreamDelta { Op = DexChatPatchOp.Append, Path = "/messages/0/content/parts/0/value", Value = "hello" };
        var json = JsonSerializer.Serialize(frame, Json);
        Assert.Contains("\"type\":\"delta\"", json);
        Assert.Contains("\"op\":\"append\"", json);
        Assert.Contains("\"path\":\"/messages/0/content/parts/0/value\"", json);
        Assert.Contains("\"value\":\"hello\"", json);
    }

    [Fact]
    public void Delta_value_serializes_by_runtime_type() {
        DexChatResponseUpdate frame = new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/messages/0/content/parts/-", Value = ChatMessagePart.FromText("", "text/markdown") };
        var json = JsonSerializer.Serialize(frame, Json);
        Assert.Contains("\"contentType\":\"text/markdown\"", json);
    }

    [Fact]
    public void Compacted_delta_omits_inherited_path_and_op() {
        DexChatResponseUpdate frame = new DexChatStreamDelta { Value = "tok" }; // path/op inherited from previous delta
        Assert.Equal("{\"type\":\"delta\",\"value\":\"tok\"}", JsonSerializer.Serialize(frame, Json));
    }

    [Fact]
    public void Done_frame_is_a_bare_marker() {
        DexChatResponseUpdate frame = new DexChatStreamDone();
        Assert.Equal("{\"type\":\"done\"}", JsonSerializer.Serialize(frame, Json));
    }
}
