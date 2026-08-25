#if NET9_0_OR_GREATER
using Indice.AspNetCore.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Tests;

public class MagicBytesUploadTests : IAsyncLifetime
{
    private const string BASE_URL = "https://server";

    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;
    private ServiceProvider _serviceProvider;

    public MagicBytesUploadTests(ITestOutputHelper output)
    {
        _output = output;
        var builder = new WebHostBuilder();
        builder.ConfigureServices((context, services) =>
        {
            services.AddRouting();
            services.AddMagicBytesValidator();
            services.Configure<LimitUploadOptions>(o =>
            {
                o.EnableMagicByteValidation = true;
                o.AllowUnknownExtensions = false;
            });
        });
        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(e => e.MapMagicBytesUploadTestEndpoints());
        });
        var server = new TestServer(builder);
        _httpClient = new HttpClient(server.CreateHandler()) { BaseAddress = new Uri(BASE_URL) };
        _serviceProvider = (ServiceProvider)server.Services;
    }

    public async ValueTask DisposeAsync() => await _serviceProvider.DisposeAsync();
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    [Theory]
    [MemberData(nameof(GetValidFilePaths))]
    public async Task Upload_RealImageFile_ValidMagicBytes_ReturnsOk(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _output.WriteLine($"Skipping — file not found: {filePath}");
            return;
        }
        var originalBytes = await File.ReadAllBytesAsync(filePath, TestContext.Current.CancellationToken);
        var response = await PostFileAsync(filePath);

        if (!response.IsSuccessStatusCode)
            _output.WriteLine(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.True(response.IsSuccessStatusCode);
        var returnedBytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(originalBytes, returnedBytes);
    }

    private async Task<HttpResponseMessage> PostFileAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var contentType = GetContentType(fileName);

        await using var fileStream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(Path.GetFileNameWithoutExtension(fileName)), "name");

        return await _httpClient.PostAsync($"tests/upload", content);
    }

    // Full paths to valid files
    public static TheoryData<string> GetValidFilePaths() => new()
    {
        Path.Combine("Assets", "Images", "sample.jpg"),
        Path.Combine("Assets", "Images", "sample.png"),
        Path.Combine("Assets", "Images", "sample.svg"),
        Path.Combine("Assets", "Images", "sample.webp"),
    };

    private static string GetContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            ".bmp"            => "image/bmp",
            ".pdf"            => "application/pdf",
            _                 => "application/octet-stream"
        };
}

public static class MagicBytesUploadTestEndpoints
{
    public static IEndpointRouteBuilder MapMagicBytesUploadTestEndpoints(this IEndpointRouteBuilder routes)
    {
        var maxSizeLimit = 4 * 1024 * 1024; // 4 MB
        var group = routes.MapGroup("tests");

        group.MapPost("upload", async (IFormFile file) => {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                return Results.File(ms.ToArray(), file.ContentType, file.FileName);
            })
            .DisableAntiforgery()
            .WithName("MagicBytesUpload")
            .LimitUpload(sizeLimit: maxSizeLimit, fileExtensions: "pdf, svg, docx, jpg, jpeg, png, gif, webp", enableMagicByteValidation: true);

        return routes;
    }
}
#endif