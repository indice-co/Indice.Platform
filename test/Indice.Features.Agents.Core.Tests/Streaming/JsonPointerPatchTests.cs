using System.Text.Json.Nodes;
using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Tests.Streaming;

public class JsonPointerPatchTests
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = DexChatStreamFrameSerializationTests.Json;

    private static JsonObject Apply(params DexChatStreamDelta[] frames) {
        var document = new JsonObject();
        var applier = new JsonPointerPatch(); // one instance per stream — carries the compaction state
        foreach (var frame in frames) { applier.Apply(document, frame, Json); }
        return document;
    }

    [Fact]
    public void Add_creates_root_member() {
        var doc = Apply(new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/limitReached", Value = false });
        Assert.False(doc["limitReached"]!.GetValue<bool>());
    }

    [Fact]
    public void Add_replaces_existing_member() {
        var doc = Apply(
            new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/modelId", Value = "a" },
            new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/modelId", Value = "b" });
        Assert.Equal("b", doc["modelId"]!.GetValue<string>());
    }

    [Fact]
    public void Add_with_dash_appends_to_array() {
        var doc = Apply(
            new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/items", Value = new List<string>() },
            new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/items/-", Value = "x" });
        Assert.Equal("x", doc["items"]![0]!.GetValue<string>());
    }

    [Fact]
    public void Append_concatenates_strings_along_a_deep_pointer() {
        var doc = Apply(
            new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/messages", Value = new List<object> { new { content = new { parts = new List<object>() } } } },
            new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/messages/0/content/parts/-", Value = ChatMessagePart.FromText("", "text/markdown") },
            new DexChatStreamDelta { Op = DexChatPatchOp.Append, Path = "/messages/0/content/parts/0/value", Value = "Hello " },
            new DexChatStreamDelta { Op = DexChatPatchOp.Append, Path = "/messages/0/content/parts/0/value", Value = "world" });
        Assert.Equal("Hello world", doc["messages"]![0]!["content"]!["parts"]![0]!["value"]!.GetValue<string>());
    }

    [Fact]
    public void Replace_requires_existing_member() {
        Assert.Throws<InvalidOperationException>(() => Apply(new DexChatStreamDelta { Op = DexChatPatchOp.Replace, Path = "/missing", Value = "x" }));
    }

    [Fact]
    public void Omitted_path_and_op_inherit_from_previous_delta() {
        var doc = Apply(
            new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/text", Value = "" },
            new DexChatStreamDelta { Op = DexChatPatchOp.Append, Value = "Hello " }, // op changed, path inherited
            new DexChatStreamDelta { Value = "world" });                              // both inherited
        Assert.Equal("Hello world", doc["text"]!.GetValue<string>());
    }

    [Fact]
    public void First_delta_without_path_or_op_throws() {
        Assert.Throws<InvalidOperationException>(() => Apply(new DexChatStreamDelta { Value = "x" }));
    }

    [Fact]
    public void Pointer_unescapes_rfc6901_tokens() {
        var doc = Apply(
            new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/a~1b", Value = 1 },
            new DexChatStreamDelta { Op = DexChatPatchOp.Add, Path = "/c~0d", Value = 2 });
        Assert.Equal(1, doc["a/b"]!.GetValue<int>());
        Assert.Equal(2, doc["c~d"]!.GetValue<int>());
    }
}
