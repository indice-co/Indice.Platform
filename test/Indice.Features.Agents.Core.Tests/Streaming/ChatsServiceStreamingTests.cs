using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;
using Indice.Types;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Tests.Streaming;

public class ChatsServiceStreamingTests
{
    private static readonly JsonSerializerOptions Json = DexChatStreamFrameSerializationTests.Json;
    private static readonly Guid ConversationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string PersistedMessageId = "22222222-2222-2222-2222-222222222222";

    // The canonical fixture mirrors the live pipeline: status, two text tokens, the trailing annotations-only
    // empty carrier (AnswerComposer's citation shape), then the metadata-only final update.
    private static List<ChatResponseUpdate> DefaultUpdates() {
        var citation = new Citation { ChunkId = Guid.Parse("33333333-3333-3333-3333-333333333333"), Title = "Doc", Number = 1 };
        return [
            new ChatResponseUpdate(ChatRole.Assistant, [new StepProgressContent("Retrieving relevant context")]),
            new ChatResponseUpdate(ChatRole.Assistant, "Hello "),
            new ChatResponseUpdate(ChatRole.Assistant, "world"),
            new ChatResponseUpdate(ChatRole.Assistant, [new TextContent(string.Empty) {
                Annotations = [new CitationAnnotation { Title = citation.Title, FileId = citation.ChunkId.ToString(), RawRepresentation = citation }]
            }]),
            new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5, TotalTokenCount = 15 })]) {
                ResponseId = "resp-1", ModelId = "gpt-test", FinishReason = ChatFinishReason.Stop, CreatedAt = DateTimeOffset.UnixEpoch
            }
        ];
    }

    private static (ChatsService Service, FakeConversationStore Store) CreateService(List<ChatResponseUpdate>? updates = null, bool limitReached = false) {
        var store = new FakeConversationStore(ConversationId, PersistedMessageId);
        var client = new FakeDexChatClient(updates ?? DefaultUpdates());
        var service = new ChatsService(store, client, new FakeUsageGuard(limitReached), Options.Create(new AgentsOptions()), NullLogger<ChatsService>.Instance);
        return (service, store);
    }

    private static async Task<List<SseItem<DexChatResponseUpdate>>> Collect(ChatsService service) {
        var stream = await service.SendStreamAsync("user-1", null, new ChatRequest { Text = "hi" }, CancellationToken.None);
        var frames = new List<SseItem<DexChatResponseUpdate>>();
        await foreach (var item in stream!) { frames.Add(item); }
        return frames;
    }

    [Fact]
    public async Task Grammar_start_patches_done_with_correct_sse_event_names() {
        var (service, _) = CreateService();
        var frames = await Collect(service);
        Assert.IsType<DexChatStreamStart>(frames[0].Data);
        Assert.IsType<DexChatStreamDone>(frames[^1].Data);
        foreach (var frame in frames) {
            var expected = frame.Data is DexChatStreamDelta ? "delta" : "message"; // SseItem defaults to the 'message' event
            Assert.Equal(expected, frame.EventType);
        }
        Assert.Contains(frames, frame => frame.Data is DexChatStreamStatus status && status.Value == "Retrieving relevant context");
    }

    [Fact]
    public async Task Invariant_patches_assemble_into_the_nonstreaming_response() {
        var (streaming, _) = CreateService();
        var document = new JsonObject();
        var applier = new JsonPointerPatch(); // per-stream instance — inflates compacted frames
        foreach (var patch in (await Collect(streaming)).Select(item => item.Data).OfType<DexChatStreamDelta>()) {
            applier.Apply(document, patch, Json);
        }
        var (nonStreaming, _) = CreateService(); // identical fixture, fresh store ⇒ same canonical response
        var canonical = await nonStreaming.SendAsync("user-1", null, new ChatRequest { Text = "hi" }, CancellationToken.None);
        var expected = Normalize(JsonSerializer.SerializeToNode(canonical, Json)!.AsObject());
        expected.Remove("text"); // computed convenience member — never patched
        Assert.True(JsonNode.DeepEquals(Normalize(document), expected),
            $"assembled:\n{document.ToJsonString()}\nexpected:\n{expected.ToJsonString()}");
    }

    [Fact]
    public async Task Consecutive_appends_compact_path_and_op() {
        var (service, _) = CreateService(); // fixture streams "Hello " then "world" → two append frames
        var appends = (await Collect(service)).Select(item => item.Data).OfType<DexChatStreamDelta>()
            .Where(delta => delta.Value is string text && (text == "Hello " || text == "world")).ToList();
        Assert.Equal(2, appends.Count);
        Assert.NotNull(appends[0].Path);              // first append after the part-open frame: path changed ⇒ carried
        Assert.Equal(DexChatPatchOp.Append, appends[0].Op);
        Assert.Null(appends[1].Path);                 // identical path/op ⇒ compacted away
        Assert.Null(appends[1].Op);
    }

    [Fact]
    public async Task Citations_arrive_as_a_tail_patch() {
        var (service, _) = CreateService(); // fixture carries the citation on a trailing annotations-only empty carrier
        var frames = await Collect(service);
        Assert.Contains(frames, frame => frame.Data is DexChatStreamDelta { Path: "/messages/0/citations" });
    }

    [Fact]
    public async Task Pipeline_fault_persists_failed_turn_and_ends_with_error() {
        var (service, store) = CreateService([new ChatResponseUpdate(ChatRole.Assistant, [new ErrorContent("boom")])]);
        var frames = await Collect(service);
        Assert.IsType<DexChatStreamError>(frames[^1].Data);
        Assert.True(store.FailedTurnPersisted);
        Assert.False(store.TurnPersisted);
    }

    [Fact]
    public async Task Mixed_status_and_text_update_drops_nothing() {
        var updates = DefaultUpdates();
        updates[1] = new ChatResponseUpdate(ChatRole.Assistant, [new StepProgressContent("Composing"), new TextContent("Hello ")]);
        var (service, _) = CreateService(updates);
        var frames = await Collect(service);
        Assert.Contains(frames, frame => frame.Data is DexChatStreamStatus status && status.Value == "Composing");
        Assert.Contains(frames, frame => frame.Data is DexChatStreamDelta { Op: DexChatPatchOp.Append, Value: "Hello " });
    }

    [Fact]
    public async Task Disconnect_mid_stream_salvages_the_user_message() {
        var (service, store) = CreateService();
        var stream = await service.SendStreamAsync("user-1", null, new ChatRequest { Text = "hi" }, CancellationToken.None);
        var enumerator = stream!.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await enumerator.MoveNextAsync(); // start
        await enumerator.MoveNextAsync(); // first patch
        await enumerator.DisposeAsync();  // client went away
        Assert.True(store.FailedTurnPersisted);
        Assert.False(store.TurnPersisted);
    }

    [Fact]
    public async Task Limit_reached_streams_projected_response_and_persists_nothing() {
        var (service, store) = CreateService(limitReached: true);
        var frames = await Collect(service);
        Assert.IsType<DexChatStreamDone>(frames[^1].Data);
        Assert.Contains(frames, frame => frame.Data is DexChatStreamDelta { Path: "/limitReached", Value: true });
        Assert.Contains(frames, frame => frame.Data is DexChatStreamDelta { Path: "/messages" });
        Assert.False(store.TurnPersisted);
        Assert.False(store.FailedTurnPersisted);
    }

    /// <summary>The out-of-scope shape: prose followed by an atomic multiple-choice part in the same update.</summary>
    private static List<ChatResponseUpdate> MultipleChoiceUpdates() => [
        new ChatResponseUpdate(ChatRole.Assistant, [
            new TextContent("That is outside what I cover."),
            new DataContent(JsonSerializer.SerializeToUtf8Bytes(new MultipleChoice { Options = ["What can you tell me about faq?"] }),
                            AgentsConstants.MediaTypes.MultipleChoice)
        ]),
        new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(new UsageDetails { TotalTokenCount = 5 })]) {
            ResponseId = "resp-1", ModelId = "gpt-test", FinishReason = ChatFinishReason.Stop, CreatedAt = DateTimeOffset.UnixEpoch
        }
    ];

    [Fact]
    public async Task Multiple_choice_part_streams_as_raw_json_beside_its_own_text_part() {
        var (service, _) = CreateService(MultipleChoiceUpdates());
        var parts = (await Collect(service)).Select(item => item.Data).OfType<DexChatStreamDelta>()
            .Where(delta => delta.Path == "/messages/0/content/parts/-")
            .Select(delta => Assert.IsType<ChatMessagePart>(delta.Value)).ToList();
        Assert.Equal(2, parts.Count);                                  // the prose part, then the atomic choice part
        Assert.Equal("text/markdown", parts[0].ContentType);
        Assert.Equal(AgentsConstants.MediaTypes.MultipleChoice, parts[1].ContentType);
        Assert.Equal("""{"options":["What can you tell me about faq?"]}""", parts[1].Value);
        Assert.DoesNotContain("base64", parts[1].Value);               // raw JSON, not the data: URI
    }

    [Fact]
    public async Task Multiple_choice_invariant_streamed_parts_equal_the_nonstreaming_ones() {
        var (streaming, _) = CreateService(MultipleChoiceUpdates());
        var document = new JsonObject();
        var applier = new JsonPointerPatch();
        foreach (var patch in (await Collect(streaming)).Select(item => item.Data).OfType<DexChatStreamDelta>()) {
            applier.Apply(document, patch, Json);
        }
        var (nonStreaming, _) = CreateService(MultipleChoiceUpdates());
        var canonical = await nonStreaming.SendAsync("user-1", null, new ChatRequest { Text = "hi" }, CancellationToken.None);
        var expected = Normalize(JsonSerializer.SerializeToNode(canonical, Json)!.AsObject());
        expected.Remove("text");
        Assert.True(JsonNode.DeepEquals(Normalize(document), expected),
            $"assembled:\n{document.ToJsonString()}\nexpected:\n{expected.ToJsonString()}");
    }

    /// <summary>A hosted-image shape: prose, an image referenced by URL, then more prose.</summary>
    private static List<ChatResponseUpdate> HostedImageUpdates() => [
        new ChatResponseUpdate(ChatRole.Assistant, [
            new TextContent("Here is the enrolment flow."),
            new UriContent("https://cdn.example.com/figures/enrolment.png", "image/png"),
            new TextContent("Anything else?")
        ]),
        new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(new UsageDetails { TotalTokenCount = 5 })]) {
            ResponseId = "resp-1", ModelId = "gpt-test", FinishReason = ChatFinishReason.Stop, CreatedAt = DateTimeOffset.UnixEpoch
        }
    ];

    [Fact]
    public async Task Hosted_image_part_streams_as_its_url_and_splits_the_prose_around_it() {
        var (service, _) = CreateService(HostedImageUpdates());
        // A repeated path is nulled on the wire, so inflate by carrying the last effective one forward — exactly what a
        // client does. Three consecutive part-adds would otherwise look like one.
        string? path = null;
        var parts = new List<ChatMessagePart>();
        foreach (var delta in (await Collect(service)).Select(item => item.Data).OfType<DexChatStreamDelta>()) {
            path = delta.Path ?? path;
            if (path == "/messages/0/content/parts/-") { parts.Add(Assert.IsType<ChatMessagePart>(delta.Value)); }
        }
        Assert.Equal(3, parts.Count);                                  // prose, the atomic image part, then fresh prose
        Assert.Equal("text/markdown", parts[0].ContentType);
        Assert.Equal("image/png", parts[1].ContentType);
        Assert.Equal("https://cdn.example.com/figures/enrolment.png", parts[1].Value);  // the URL, never base64 bytes
        Assert.Equal("text/markdown", parts[2].ContentType);
    }

    [Fact]
    public async Task Hosted_image_invariant_streamed_parts_equal_the_nonstreaming_ones() {
        var (streaming, _) = CreateService(HostedImageUpdates());
        var document = new JsonObject();
        var applier = new JsonPointerPatch();
        foreach (var patch in (await Collect(streaming)).Select(item => item.Data).OfType<DexChatStreamDelta>()) {
            applier.Apply(document, patch, Json);
        }
        var (nonStreaming, _) = CreateService(HostedImageUpdates());
        var canonical = await nonStreaming.SendAsync("user-1", null, new ChatRequest { Text = "hi" }, CancellationToken.None);
        var expected = Normalize(JsonSerializer.SerializeToNode(canonical, Json)!.AsObject());
        expected.Remove("text");
        Assert.True(JsonNode.DeepEquals(Normalize(document), expected),
            $"assembled:\n{document.ToJsonString()}\nexpected:\n{expected.ToJsonString()}");
    }

    /// <summary>Strips null-valued members recursively — the profile only patches non-null members.</summary>
    private static JsonObject Normalize(JsonObject node) {
        foreach (var key in node.Where(pair => pair.Value is null).Select(pair => pair.Key).ToList()) { node.Remove(key); }
        foreach (var child in node) {
            if (child.Value is JsonObject childObject) { Normalize(childObject); }
            if (child.Value is JsonArray array) {
                foreach (var element in array.OfType<JsonObject>()) { Normalize(element); }
            }
        }
        return node;
    }
}

internal sealed class FakeDexChatClient(List<ChatResponseUpdate> updates) : IDexChatClient
{
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => Task.FromResult(updates.ToChatResponse());

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
        foreach (var update in updates) {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return update;
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

internal sealed class FakeConversationStore(Guid persistedConversationId, string persistedMessageId) : IConversationStore
{
    public bool TurnPersisted { get; private set; }
    public bool FailedTurnPersisted { get; private set; }

    public Task<Conversation?> LoadOrCreateAsync(string userId, string? authorName,
        Guid? conversationId, CancellationToken cancellationToken)
        => Task.FromResult<Conversation?>(new Conversation { Id = persistedConversationId });

    public Task<ChatMessage> AppendTurnAsync(Guid id, ChatMessage userMessage, ChatResponse response, CancellationToken cancellationToken) {
        TurnPersisted = true;
        var assistant = response.Messages.Last();
        assistant.MessageId = persistedMessageId;
        return Task.FromResult(assistant);
    }

    public Task AppendFailedTurnAsync(Guid id, ChatMessage userMessage, CancellationToken cancellationToken) {
        FailedTurnPersisted = true;
        return Task.CompletedTask;
    }

    public Task<Conversation?> GetAsync(Guid id, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<IReadOnlyList<ChatMessage>> GetHistoryAsync(Guid id, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<ResultSet<ConversationListItem>> ListAsync(string userId, ListOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<int> DeleteAsync(Guid id, string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<int> CountSessionsAsync(string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<long> GetUsageTokensAsync(string userId, DateTimeOffset since, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<bool> SetLikeAsync(string userId, Guid id, Guid messageId, bool? liked, CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class FakeUsageGuard(bool limitReached) : IUsageGuardService
{
    public UsageGuardResult Check(Conversation session) => limitReached ? UsageGuardResult.Deny("Limit reached.") : UsageGuardResult.Allow();
    public Task<UsageGuardResult> CheckConversationCreationAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(UsageGuardResult.Allow());
}
