using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Services;

namespace Indice.Features.Agents.Core.Tests.Streaming;

public class DexChatStreamProjectorTests
{
    [Fact]
    public void First_text_opens_skeleton_part_and_appends() {
        var projector = new DexChatStreamProjector();
        var frames = projector.AppendText("Hello").ToList();
        Assert.Equal(3, frames.Count);
        Assert.Equal((DexChatPatchOp.Add, "/messages"), (frames[0].Op, frames[0].Path));
        Assert.Equal((DexChatPatchOp.Add, "/messages/0/content/parts/-"), (frames[1].Op, frames[1].Path));
        Assert.Equal((DexChatPatchOp.Append, "/messages/0/content/parts/0/value"), (frames[2].Op, frames[2].Path));
        Assert.Equal("Hello", frames[2].Value);
    }

    [Fact]
    public void Subsequent_text_is_a_single_append() {
        var projector = new DexChatStreamProjector();
        _ = projector.AppendText("Hello").ToList();
        var frames = projector.AppendText(" world").ToList();
        var frame = Assert.Single(frames);
        Assert.Equal((DexChatPatchOp.Append, "/messages/0/content/parts/0/value"), (frame.Op, frame.Path));
    }

    [Fact]
    public void Atomic_part_closes_text_part_and_next_text_opens_a_new_one() {
        var projector = new DexChatStreamProjector();
        _ = projector.AppendText("before").ToList();
        var image = projector.AddPart(ChatMessagePart.FromText("data:image/png;base64,AAAA", "image/png")).ToList();
        var frame = Assert.Single(image);
        Assert.Equal((DexChatPatchOp.Add, "/messages/0/content/parts/-"), (frame.Op, frame.Path));
        var after = projector.AppendText("after").ToList();
        Assert.Equal(2, after.Count); // opens parts/2, appends to it
        Assert.Equal("/messages/0/content/parts/2/value", after[1].Path);
    }

    [Fact]
    public void Complete_emits_message_tail_then_root_tail_skipping_nulls_and_empties() {
        var projector = new DexChatStreamProjector();
        _ = projector.AppendText("answer").ToList();
        var response = new DexChatResponse {
            ConversationId = Guid.NewGuid(),
            Messages = [new DexChatMessage { MessageId = "m-1", Role = DexChatRole.Assistant, Content = new ChatMessageContent("answer") }],
            FinishReason = DexChatFinishReason.Stop,
            Usage = new DexChatUsage { QuestionsUsedCount = 1, QuestionsLimitCount = 20 }
        };
        var paths = projector.Complete(response).Select(frame => frame.Path).ToList();
        Assert.Contains("/messages/0/messageId", paths);
        Assert.Contains("/messages/0/role", paths);
        Assert.Contains("/finishReason", paths);
        Assert.Contains("/usage", paths);
        Assert.Contains("/limitReached", paths);
        Assert.DoesNotContain("/messages/0/authorName", paths);  // null → omitted
        Assert.DoesNotContain("/messages/0/citations", paths);   // empty → omitted
        Assert.DoesNotContain("/responseId", paths);             // null → omitted
    }

    [Fact]
    public void ProjectResponse_emits_full_document_for_unstreamed_responses() {
        var response = new DexChatResponse {
            ConversationId = Guid.NewGuid(),
            Messages = [new DexChatMessage { MessageId = Guid.Empty.ToString(), Role = DexChatRole.Assistant, Content = new ChatMessageContent("limit reached") }],
            FinishReason = DexChatFinishReason.Limit,
            LimitReached = true,
            Usage = new DexChatUsage { QuestionsUsedCount = 20, QuestionsLimitCount = 20 }
        };
        var frames = new DexChatStreamProjector().ProjectResponse(response).ToList();
        Assert.Equal((DexChatPatchOp.Add, "/conversationId"), (frames[0].Op, frames[0].Path));
        Assert.Equal((DexChatPatchOp.Add, "/messages"), (frames[1].Op, frames[1].Path));
        Assert.Same(response.Messages, frames[1].Value); // full typed messages — nothing was streamed
        Assert.Contains(frames, frame => frame.Path == "/limitReached" && true.Equals(frame.Value));
    }
}
