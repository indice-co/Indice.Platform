using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Tests;

public class DexChatConversionTests
{
    [Fact]
    public void ToDexChatResponse_MapsIdentityUsageAndText() {
        var conversationId = Guid.NewGuid();
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello **world**") {
            MessageId = "assistant-1",
            AuthorName = "dex",
            CreatedAt = DateTimeOffset.UnixEpoch
        }) {
            ConversationId = conversationId.ToString(),
            ResponseId = "resp-1",
            ModelId = "gpt-4o",
            FinishReason = ChatFinishReason.Stop,
            Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 20, TotalTokenCount = 30 }
        };

        var dex = response.ToDexChatResponse();

        Assert.Equal(conversationId, dex.ConversationId);
        Assert.Equal("resp-1", dex.ResponseId);
        Assert.Equal("gpt-4o", dex.ModelId);
        Assert.Equal(DexChatFinishReason.Stop, dex.FinishReason);
        Assert.Equal(10, dex.Usage!.InputTokenCount);
        Assert.Equal(30, dex.Usage.TotalTokenCount);
        var message = Assert.Single(dex.Messages);
        Assert.Equal(DexChatRole.Assistant, message.Role);
        Assert.Equal("assistant-1", message.MessageId);
        Assert.Equal("dex", message.AuthorName);
        Assert.Equal(DateTimeOffset.UnixEpoch, message.CreatedAt);
        var part = Assert.Single(message.Content.Parts);
        Assert.Equal("Hello **world**", part.Value);
        Assert.Equal("Hello **world**", dex.Text);
        Assert.False(dex.LimitReached);
        Assert.Empty(message.Sources);
    }

    [Fact]
    public void ToDexChatMessage_LiftsCitationsFromAnnotations() {
        var citation = new Citation { ChunkId = Guid.NewGuid(), DocumentId = Guid.NewGuid(), Title = "Doc", Number = 1, Score = 0.9 };
        var message = new ChatMessage(ChatRole.Assistant, [
            // Annotations are lifted from non-empty text contents only (the converter's guard skips empty carriers).
            new TextContent("Answer<sup>[1]</sup>") {
                Annotations = [new CitationAnnotation { Title = citation.Title, FileId = citation.ChunkId.ToString(), RawRepresentation = citation }]
            },
            new TextContent(string.Empty)
        ]);

        var dex = message.ToDexChatMessage();

        var lifted = Assert.Single(dex.Citations);
        Assert.Same(citation, lifted);
        var part = Assert.Single(dex.Content.Parts); // the empty annotation carrier must not become a part
        Assert.Equal("Answer<sup>[1]</sup>", part.Value);
        Assert.Empty(dex.Sources);
    }

    [Theory]
    [InlineData("user", DexChatRole.User)]
    [InlineData("assistant", DexChatRole.Assistant)]
    [InlineData("system", DexChatRole.System)]
    [InlineData("tool", DexChatRole.Tool)]
    [InlineData("unknown-role", DexChatRole.User)]
    public void ToDexChatRole_MapsEveryRole(string role, DexChatRole expected)
        => Assert.Equal(expected, new ChatRole(role).ToDexChatRole());

    [Theory]
    [InlineData("stop", DexChatFinishReason.Stop)]
    [InlineData("length", DexChatFinishReason.Length)]
    [InlineData("tool_calls", DexChatFinishReason.ToolCalls)]
    [InlineData("content_filter", DexChatFinishReason.ContentFilter)]
    [InlineData("limit", DexChatFinishReason.Limit)]
    [InlineData("weird", null)]
    public void ToDexChatFinishReason_MapsEveryReason(string reason, DexChatFinishReason? expected)
        => Assert.Equal(expected, ((ChatFinishReason?)new ChatFinishReason(reason)).ToDexChatFinishReason());

    [Fact]
    public void ToDexChatFinishReason_NullStaysNull()
        => Assert.Null(((ChatFinishReason?)null).ToDexChatFinishReason());
}
