using System.Text;
using System.Text.Json;
using Indice.EntityFrameworkCore.ValueConversion;
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
            new TextContent("Answer<sup>[1]</sup>"),
            // The pipeline's live shape: citations arrive on a trailing annotations-only empty carrier
            // (AnswerComposer stamps them once the full text and exact offsets are known).
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
    public void ToDexChatMessage_NumbersRoundTrippedCitationsSequentially() {
        // Persisted contents lose RawRepresentation (it does not serialize), so rebuilt citations carry no Number.
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var message = new ChatMessage(ChatRole.Assistant, [
            new TextContent("Answer<sup>[1][2]</sup>"),
            new TextContent(string.Empty) {
                Annotations = [
                    new CitationAnnotation { Title = "First", FileId = first.ToString() },
                    new CitationAnnotation { Title = "Second", FileId = second.ToString() }
                ]
            }
        ]);

        var citations = message.ToDexChatMessage().Citations;

        Assert.Equal(2, citations.Count);
        Assert.Equal((first, 1), (citations[0].ChunkId, citations[0].Number));
        Assert.Equal((second, 2), (citations[1].ChunkId, citations[1].Number));
    }

    [Fact]
    public void ToChatMessagePart_JsonPayloadCarriesRawJson() {
        var json = """{"options":["one","two"]}""";
        var data = new DataContent(Encoding.UTF8.GetBytes(json), AgentsConstants.MediaTypes.MultipleChoice);

        var part = data.ToChatMessagePart();

        Assert.Equal(AgentsConstants.MediaTypes.MultipleChoice, part.ContentType);
        Assert.Equal(json, part.Value); // the client parses this directly — never a base64 data: URI
    }

    [Fact]
    public void ToChatMessagePart_BinaryPayloadKeepsTheDataUri() {
        var data = new DataContent(new byte[] { 1, 2, 3 }, "image/png");

        var part = data.ToChatMessagePart();

        Assert.Equal("image/png", part.ContentType);
        Assert.StartsWith("data:image/png;base64,", part.Value);
    }

    [Fact]
    public void ToDexChatMessage_MultipleChoiceDataContentBecomesItsOwnPart() {
        var json = """{"options":["What can you tell me about faq?"]}""";
        var message = new ChatMessage(ChatRole.Assistant, [
            new TextContent("That is outside what I cover."),
            new DataContent(Encoding.UTF8.GetBytes(json), AgentsConstants.MediaTypes.MultipleChoice),
            new TextContent("Anything else?")
        ]);

        var parts = message.ToDexChatMessage().Content.Parts;

        // The data part closes the open text part, so the trailing prose opens a new one rather than merging back.
        Assert.Equal(3, parts.Count);
        Assert.Equal(("text/markdown", "That is outside what I cover."), (parts[0].ContentType, parts[0].Value));
        Assert.Equal((AgentsConstants.MediaTypes.MultipleChoice, json), (parts[1].ContentType, parts[1].Value));
        Assert.Equal(("text/markdown", "Anything else?"), (parts[2].ContentType, parts[2].Value));
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

    [Fact]
    public void ToChatMessagePart_UriContentCarriesTheAbsoluteUrl() {
        var uri = new UriContent("https://cdn.example.com/figures/enrolment.png", "image/png");

        var part = uri.ToChatMessagePart();

        Assert.Equal("image/png", part.ContentType);
        Assert.Equal("https://cdn.example.com/figures/enrolment.png", part.Value); // never base64 — the bytes stay on the CDN
    }

    [Fact]
    public void ToDexChatMessage_UriContentBecomesItsOwnPart() {
        var message = new ChatMessage(ChatRole.Assistant, [
            new TextContent("Here is the enrolment flow."),
            new UriContent("https://cdn.example.com/figures/enrolment.png", "image/png"),
            new TextContent("Anything else?")
        ]);

        var parts = message.ToDexChatMessage().Content.Parts;

        // Like a data part, the uri part closes the open text part so the trailing prose opens a new one.
        Assert.Equal(3, parts.Count);
        Assert.Equal(("text/markdown", "Here is the enrolment flow."), (parts[0].ContentType, parts[0].Value));
        Assert.Equal(("image/png", "https://cdn.example.com/figures/enrolment.png"), (parts[1].ContentType, parts[1].Value));
        Assert.Equal(("text/markdown", "Anything else?"), (parts[2].ContentType, parts[2].Value));
    }

    [Fact]
    public void UriContent_RoundTripsThroughTheContentsJsonColumn() {
        // DbMessage.Contents is persisted with exactly these options. UriContent is in MEAI's [JsonDerivedType] list,
        // so an image attached by URL survives a history reload with no custom converter and no migration.
        var options = JsonStringValueConverter<List<AIContent>>.SerializerOptions;
        List<AIContent> contents = [new UriContent("https://cdn.example.com/figures/enrolment.png", "image/png")];

        var rehydrated = JsonSerializer.Deserialize<List<AIContent>>(JsonSerializer.Serialize(contents, options), options);

        var uri = Assert.IsType<UriContent>(Assert.Single(rehydrated!));
        Assert.Equal("https://cdn.example.com/figures/enrolment.png", uri.Uri.ToString());
        Assert.Equal("image/png", uri.MediaType);
    }

    [Fact]
    public void DataContentName_RoundTripsThroughTheContentsJsonColumn() {
        // A bare image/png part carries its caption as the part's name, which only works if MEAI writes
        // DataContent.Name into the same JSON column DbMessage.Contents is persisted with. If it did not,
        // every such caption would vanish on the first history reload.
        var options = JsonStringValueConverter<List<AIContent>>.SerializerOptions;
        List<AIContent> contents = [new DataContent(new byte[] { 1, 2, 3 }, "image/png") { Name = "A diagram" }];

        var rehydrated = JsonSerializer.Deserialize<List<AIContent>>(JsonSerializer.Serialize(contents, options), options);

        var data = Assert.IsType<DataContent>(Assert.Single(rehydrated!));
        Assert.Equal("A diagram", data.Name);
        Assert.Equal("image/png", data.MediaType);
    }

    [Fact]
    public void ToChatMessagePart_LiftsTheDataContentNameOntoThePart() {
        // The name is how a bare image/png part carries its caption, so it has to survive both projection branches —
        // the +json one that decodes the payload and the binary one that emits the data: URI.
        var json = new DataContent(Encoding.UTF8.GetBytes("""{"options":["one"]}"""), AgentsConstants.MediaTypes.MultipleChoice) { Name = "Suggestions" };
        var binary = new DataContent(new byte[] { 1, 2, 3 }, "image/png") { Name = "A diagram" };

        Assert.Equal("Suggestions", json.ToChatMessagePart().Name);
        Assert.Equal("A diagram", binary.ToChatMessagePart().Name);
    }

    [Fact]
    public void ToChatMessagePart_LeavesTheNameNullWhenTheContentHasNone() {
        Assert.Null(new DataContent(new byte[] { 1, 2, 3 }, "image/png").ToChatMessagePart().Name);
        Assert.Null(new UriContent("https://cdn.example.com/a.png", "image/png").ToChatMessagePart().Name);
    }
}
