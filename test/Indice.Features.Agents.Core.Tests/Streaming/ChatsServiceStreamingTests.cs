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
        foreach (var item in await Collect(streaming)) {
            if (item.Data is DexChatStreamDelta patch) { applier.Apply(document, patch, Json); }
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
        var enumerator = stream!.GetAsyncEnumerator();
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

internal sealed class FakeConversationStore(Guid conversationId, string persistedMessageId) : IConversationStore
{
    public bool TurnPersisted { get; private set; }
    public bool FailedTurnPersisted { get; private set; }

    public Task<Conversation?> LoadOrCreateAsync(string userId, Guid? id, CancellationToken cancellationToken)
        => Task.FromResult<Conversation?>(new Conversation { Id = conversationId });

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
