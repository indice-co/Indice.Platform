using FluentValidation.TestHelper;
using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Server.Endpoints;
using Indice.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Indice.Features.Agents.Server.Tests;

public class ChatRequestValidatorTests
{
    private readonly ChatRequestValidator _validator = new(new MagicBytesValidator(NullLogger<MagicBytesValidator>.Instance));

    // Valid PNG header followed by padding bytes.
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] NotPngBytes = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];

    private static string ToDataUri(string mediaType, byte[] bytes) => $"data:{mediaType};base64,{Convert.ToBase64String(bytes)}";

    private static ChatMessagePart Part(string contentType, string value) => new() { ContentType = contentType, Value = value };

    [Fact]
    public async Task Valid_Text_Request_Passes() {
        var request = new ChatRequest { Text = "Hello there" };
        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task More_Than_Five_Parts_Fails() {
        var request = new ChatRequest { Text = "Hello" };
        for (var i = 0; i < 5; i++) {
            request.Parts.Add(Part("application/json", "{}"));
        }
        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        result.ShouldHaveValidationErrorFor(x => x.Parts);
    }

    [Fact]
    public async Task More_Than_One_Text_Part_Fails() {
        var request = new ChatRequest { Text = "Hello" };
        request.Parts.Add(Part("text/markdown", "# extra"));
        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        result.ShouldHaveValidationErrorFor(x => x.Parts);
    }

    [Theory]
    [InlineData("application/xml")]
    [InlineData("application/pdf")]
    [InlineData("video/mp4")]
    public async Task Disallowed_Content_Type_Fails(string contentType) {
        var request = new ChatRequest { Text = "Hello" };
        request.Parts.Add(Part(contentType, "some value"));
        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("ContentType"));
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/vnd.indice.v1+json")]
    public async Task Valid_Json_Part_Passes(string contentType) {
        var request = new ChatRequest { Text = "Hello" };
        request.Parts.Add(Part(contentType, """{"key": "value"}"""));
        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/vnd.indice.v1+json")]
    public async Task Malformed_Json_Part_Fails(string contentType) {
        var request = new ChatRequest { Text = "Hello" };
        request.Parts.Add(Part(contentType, "{ not valid json"));
        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("Value"));
    }

    [Fact]
    public async Task Valid_Png_Image_Part_Passes() {
        var request = new ChatRequest { Text = "Hello" };
        request.Parts.Add(Part("image/png", ToDataUri("image/png", PngBytes)));
        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Spoofed_Png_Image_Part_Fails() {
        var request = new ChatRequest { Text = "Hello" };
        request.Parts.Add(Part("image/png", ToDataUri("image/png", NotPngBytes)));
        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("Value"));
    }

    [Fact]
    public async Task Image_Part_Without_Data_Uri_Fails() {
        var request = new ChatRequest { Text = "Hello" };
        request.Parts.Add(Part("image/png", Convert.ToBase64String(PngBytes)));
        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("Value"));
    }

    [Fact]
    public async Task Image_Part_With_Unresolvable_Mime_Type_Fails() {
        var request = new ChatRequest { Text = "Hello" };
        request.Parts.Add(Part("image/unknown-thing", ToDataUri("image/unknown-thing", PngBytes)));
        var result = await _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("Value"));
    }
}
