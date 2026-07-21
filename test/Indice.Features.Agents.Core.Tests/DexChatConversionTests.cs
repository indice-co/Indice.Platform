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
            Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 20, TotalTokenCount = 30 },
            AdditionalProperties = new AdditionalPropertiesDictionary { ["traceId"] = "t-1" }
        };

        var dex = response.ToDexChatResponse();

        Assert.Equal(conversationId, dex.ConversationId);
        Assert.Equal("resp-1", dex.ResponseId);
        Assert.Equal("gpt-4o", dex.ModelId);
        Assert.Equal(ChatFinishReason.Stop, dex.FinishReason);
        Assert.Equal(10, dex.Usage!.InputTokenCount);
        Assert.Equal(30, dex.Usage.TotalTokenCount);
        Assert.Equal("t-1", dex.AdditionalProperties!["traceId"]);
        var message = Assert.Single(dex.Messages);
        Assert.Equal(ChatRole.Assistant, message.Role);
        Assert.Equal("assistant-1", message.MessageId);
        Assert.Equal("dex", message.AuthorName);
        Assert.Equal(DateTimeOffset.UnixEpoch, message.CreatedAt);
        var part = Assert.Single(message.Content.Parts);
        Assert.Equal("Hello **world**", part.Value);
        Assert.Equal("Hello **world**", dex.Text);
        Assert.False(dex.LimitReached);
        Assert.Empty(dex.Sources);
    }

    [Fact]
    public void ToDexChatMessage_LiftsCitationsFromAnnotations() {
        var citation = new Citation { ChunkId = Guid.NewGuid(), DocumentId = Guid.NewGuid(), Title = "Doc", Number = 1, Score = 0.9 };
        var message = new ChatMessage(ChatRole.Assistant, [
            new TextContent("Answer<sup>[1]</sup>"),
            // AnswerComposer emits annotations on a trailing empty TextContent — mirror that shape.
            new TextContent(string.Empty) {
                Annotations = [new CitationAnnotation { Title = citation.Title, FileId = citation.ChunkId.ToString(), RawRepresentation = citation }]
            }
        ]);

        var dex = message.ToDexChatMessage();

        var lifted = Assert.Single(dex.Citations);
        Assert.Same(citation, lifted);
        var part = Assert.Single(dex.Content.Parts); // the empty annotation carrier must not become a part
        Assert.Equal("Answer<sup>[1]</sup>", part.Value);
        Assert.Empty(dex.Sources);
    }

    [Fact]
    public void RoundTrip_PreservesCoreFields() {
        var dex = new DexChatResponse {
            ConversationId = Guid.NewGuid(),
            ResponseId = "resp-2",
            Messages = [new DexChatMessage {
                MessageId = "m-1",
                Role = ChatRole.User,
                AuthorName = "krikor",
                Content = new ChatMessageContent("How do I reset my password?"),
                CreatedAt = DateTimeOffset.UnixEpoch
            }],
            Usage = new DexChatUsage { InputTokenCount = 5, OutputTokenCount = 7 }
        };

        var roundTripped = dex.ToChatResponse().ToDexChatResponse();

        Assert.Equal(dex.ConversationId, roundTripped.ConversationId);
        Assert.Equal(dex.ResponseId, roundTripped.ResponseId);
        Assert.Equal(dex.Text, roundTripped.Text);
        var message = Assert.Single(roundTripped.Messages);
        Assert.Equal(ChatRole.User, message.Role);
        Assert.Equal("m-1", message.MessageId);
        Assert.Equal("krikor", message.AuthorName);
        Assert.Equal(5, roundTripped.Usage!.InputTokenCount);
    }
}
