using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Indice.Features.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Tests;

public class ChatRequestToAIContentTests
{
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    [Fact]
    public void ChatRequest_Parts_Convert_To_Expected_AIContent_Collection() {
        // Arrange: a request carrying every supported part shape.
        var request = new ChatRequest { Text = "Hello there" };
        request.Parts.Add(ChatMessagePart.FromImage(PngBytes, MediaTypeNames.Image.Png, name: "A png"));
        request.Parts.Add(ChatMessagePart.FromText("""{"key":"value"}""", MediaTypeNames.Application.Json));
        request.Parts.Add(ChatMessagePart.FromObject(new { city = "Athens" }, AgentsConstants.MediaTypes.FunctionCallPort.Request, "get_weather", requestId: "call-1"));
        request.Parts.Add(ChatMessagePart.FromText("Yes", AgentsConstants.MediaTypes.ConfirmationPort.Response, requestId: "confirm-1"));

        // Act
        var contents = request.Parts.Select(part => part.ToAIContent()).ToList();

        // Assert
        Assert.Equal(5, contents.Count);

        var text = Assert.IsType<TextContent>(contents[0]);
        Assert.Equal("Hello there", text.Text);

        var image = Assert.IsType<DataContent>(contents[1]);
        Assert.Equal("image/png", image.MediaType);
        Assert.Equal(PngBytes, image.Data.ToArray());

        var json = Assert.IsType<DataContent>(contents[2]);
        Assert.Equal("application/json", json.MediaType);
        Assert.Equal("""{"key":"value"}""", Encoding.UTF8.GetString(json.Data.ToArray()));

        var functionCall = Assert.IsType<FunctionCallContent>(contents[3]);
        Assert.Equal("call-1", functionCall.CallId);
        Assert.Equal("get_weather", functionCall.Name);
        Assert.NotNull(functionCall.Arguments);
        Assert.True(functionCall.Arguments!.ContainsKey("city"));

        var confirmationResult = Assert.IsType<FunctionResultContent>(contents[4]);
        Assert.Equal("confirm-1", confirmationResult.CallId);
        Assert.Equal("Yes", confirmationResult.Result);
    }

    [Fact]
    public void FunctionCallPort_Response_Converts_To_FunctionResultContent_With_Json_Result() {
        var part = new ChatMessagePart {
            ContentType = AgentsConstants.MediaTypes.FunctionCallPort.Response,
            RequestId = "call-2",
            Value = """{"temperature":21}"""
        };

        var content = Assert.IsType<FunctionResultContent>(part.ToAIContent());

        Assert.Equal("call-2", content.CallId);
        var element = Assert.IsType<JsonElement>(content.Result);
        Assert.Equal(21, element.GetProperty("temperature").GetInt32());
    }

    [Fact]
    public void ConfirmationPort_Request_Converts_To_FunctionCallContent() {
        var part = new ChatMessagePart {
            ContentType = AgentsConstants.MediaTypes.ConfirmationPort.Request,
            RequestId = "confirm-2",
            Value = """{"prompt":"Proceed with payment?"}"""
        };

        var content = Assert.IsType<FunctionCallContent>(part.ToAIContent());

        Assert.Equal("confirm-2", content.CallId);
        Assert.Equal("confirmation", content.Name);
        Assert.NotNull(content.Arguments);
    }

    [Fact]
    public void Vendor_Json_Part_Converts_To_DataContent_Preserving_Media_Type() {
        var part = new ChatMessagePart {
            ContentType = AgentsConstants.MediaTypes.Callout,
            Value = """{"title":"Heads up","severity":"info"}"""
        };

        var content = Assert.IsType<DataContent>(part.ToAIContent());

        Assert.Equal(AgentsConstants.MediaTypes.Callout, content.MediaType);
    }

    [Fact]
    public void Unsupported_Content_Type_Throws_NotSupportedException() {
        var part = new ChatMessagePart { ContentType = "video/mp4", Value = "whatever" };

        Assert.Throws<NotSupportedException>(() => part.ToAIContent());
    }
}