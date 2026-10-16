using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Features.Agents.Core.Extensions;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Tests;

public class TryGetToolResultTests
{
    private const string CaseJson = """{"caseId":"C-1","phoneNumber":"6900000000"}""";

    [Fact]
    public async Task JsonNode_ObjectIsReturned() {
        AssertCase(await CaptureAsync(JsonNode.Parse(CaseJson)));
    }

    [Fact]
    public async Task JsonElement_ObjectIsReturned() {
        AssertCase(await CaptureAsync(JsonDocument.Parse(CaseJson).RootElement));
    }

    [Fact]
    public async Task String_JsonObjectIsParsed() {
        AssertCase(await CaptureAsync(CaseJson));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ broken")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("""[{"caseId":"C-1"}]""")]
    public async Task String_MalformedBlankOrNonObject_IsUnsupported(string value) {
        await AssertUnsupportedAsync(value);
    }

    [Fact]
    public async Task TextContent_JsonObjectIsParsed() {
        AssertCase(await CaptureAsync(new TextContent(CaseJson)));
    }

    [Fact]
    public async Task TextContent_Malformed_IsUnsupported() {
        await AssertUnsupportedAsync(new TextContent("plain text"));
    }

    [Fact]
    public async Task ContentCollection_FirstJsonObjectTextIsReturned() {
        List<AIContent> contents = [
            new TextContent("plain text"),
            new TextContent("[1,2]"),
            new TextContent(CaseJson),
            new TextContent("""{"caseId":"C-2"}""")
        ];

        AssertCase(await CaptureAsync(contents));
    }

    [Fact]
    public async Task ContentCollection_WithoutJsonObject_IsUnsupported() {
        List<AIContent> contents = [new TextContent("plain text"), new DataContent(new byte[] { 1 }, "application/octet-stream")];

        await AssertUnsupportedAsync(contents);
    }

    [Fact]
    public async Task ArbitraryObject_IsSerializedWithWebDefaults() {
        AssertCase(await CaptureAsync(new CasePayload("C-1", "6900000000")));
    }

    [Fact]
    public async Task McpWrapper_StructuredContentTakesPrecedenceOverContent() {
        var wrapper = $$"""
            {
              "structuredContent": {{CaseJson}},
              "content": [ { "type": "text", "text": "{\"caseId\":\"FROM-TEXT\"}" } ]
            }
            """;

        AssertCase(await CaptureAsync(wrapper));
    }

    [Fact]
    public async Task McpWrapper_FallsBackToFirstParsableTextBlock() {
        var wrapper = """
            {
              "content": [
                { "type": "image", "data": "abc" },
                { "type": "text", "text": "not json" },
                { "type": "text", "text": "{\"caseId\":\"C-1\",\"phoneNumber\":\"6900000000\"}" },
                { "type": "text", "text": "{\"caseId\":\"C-2\"}" }
              ]
            }
            """;

        AssertCase(await CaptureAsync(wrapper));
    }

    [Fact]
    public async Task McpWrapper_NonObjectStructuredContent_FallsBackToContent() {
        var wrapper = """
            {
              "structuredContent": "oops",
              "content": [ { "type": "text", "text": "{\"caseId\":\"C-1\",\"phoneNumber\":\"6900000000\"}" } ]
            }
            """;

        AssertCase(await CaptureAsync(wrapper));
    }

    [Fact]
    public async Task McpWrapper_WithoutParsableContent_ReturnsWrapperItself() {
        var tool = await InvokeAsync("""{"content":[{"type":"text","text":"not json"}],"isError":true}""");

        Assert.True(new[] { tool }.TryGetToolResult(out var result, out var error));
        Assert.Null(error);
        Assert.True(result["isError"]!.GetValue<bool>());
    }

    [Fact]
    public void NoToolInvoked_FailsWithError() {
        var tool = new McpCapturingToolFunction(new RawResultFunction(CaseJson));

        Assert.False(new[] { tool }.TryGetToolResult(out var result, out var error));
        Assert.Null(result);
        Assert.Contains("did not invoke", error);
    }

    [Fact]
    public async Task NullResult_FailsAsNotInvoked() {
        var tool = await InvokeAsync(null);

        Assert.False(new[] { tool }.TryGetToolResult(out var result, out var error));
        Assert.Null(result);
        Assert.Contains("did not invoke", error);
    }

    [Fact]
    public async Task MultipleTools_LastCapturedResultIsUsed() {
        var first = await InvokeAsync("""{"caseId":"OLD"}""");
        var second = await InvokeAsync(CaseJson);
        var notInvoked = new McpCapturingToolFunction(new RawResultFunction("""{"caseId":"NEVER"}"""));

        Assert.True(new[] { first, second, notInvoked }.TryGetToolResult(out var result, out _));
        AssertCase(result);
    }

    private static async Task<JsonNode?> CaptureAsync(object? rawResult) {
        var tool = await InvokeAsync(rawResult);
        Assert.True(new[] { tool }.TryGetToolResult(out var result, out var error), error);
        Assert.Null(error);
        return result;
    }

    private static async Task AssertUnsupportedAsync(object rawResult) {
        var tool = await InvokeAsync(rawResult);
        Assert.False(new[] { tool }.TryGetToolResult(out var result, out var error));
        Assert.Null(result);
        Assert.Contains("Unsupported", error);
    }

    private static async Task<McpCapturingToolFunction> InvokeAsync(object? rawResult) {
        var tool = new McpCapturingToolFunction(new RawResultFunction(rawResult));
        await tool.InvokeAsync(new AIFunctionArguments());
        return tool;
    }

    private static void AssertCase(JsonNode? node) {
        var obj = Assert.IsType<JsonObject>(node);
        Assert.Equal("C-1", obj["caseId"]!.GetValue<string>());
        Assert.Equal("6900000000", obj["phoneNumber"]!.GetValue<string>());
    }

    private sealed record CasePayload(string CaseId, string PhoneNumber);

    /// <summary>Returns the given value verbatim, without any return value marshalling.</summary>
    private sealed class RawResultFunction(object? rawResult) : AIFunction
    {
        public override string Name => "get_case";

        protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken) => ValueTask.FromResult(rawResult);
    }
}
