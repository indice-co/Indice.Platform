using System.Text;
using Indice.Features.Media.AspNetCore;
using Indice.Features.Media.AspNetCore.Endpoints;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Tests;

public class MediaRootPathTests
{
    [Theory]
    [InlineData("email-assets/logo.png", "media/email-assets/logo.png")]
    [InlineData("/email-assets/fotografies/fakelos/test-file-1.txt", "media/email-assets/fotografies/fakelos/test-file-1.txt")]
    [InlineData("logo.v2.png", "media/logo.v2.png")]
    [InlineData("folder//logo.png", "media/folder/logo.png")]
    public void TryGetStoragePath_Accepts_Paths_Within_Media_Root(string path, string expected) {
        Assert.True(MediaRootPath.TryGetStoragePath(path, "media", out var storagePath));
        Assert.Equal(expected, storagePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("/")]
    [InlineData("..")]
    [InlineData("../azure-webjobs-secrets/host.json")]
    [InlineData("email-assets/../../$logs/blob/2026/11/17/0000/000000.log")]
    [InlineData("..\\azure-webjobs-secrets\\my-function-app\\host.json")]
    [InlineData("..\\..\\$logs\\blob\\2026\\11\\17\\0000\\000000.log")]
    [InlineData("email-assets\\..\\..\\secret.txt")]
    [InlineData("..%5cazure-webjobs-secrets%5chost.json")]
    [InlineData("..%2fazure-webjobs-secrets%2fhost.json")]
    [InlineData("%2e%2e/azure-webjobs-secrets/host.json")]
    [InlineData("./logo.png")]
    [InlineData("... /logo.png")]
    [InlineData(". ./logo.png")]
    [InlineData("C:/Windows/win.ini")]
    [InlineData("logo.png:secret")]
    [InlineData("logo.png?comp=list")]
    [InlineData("logo.png#fragment")]
    [InlineData("logo\u0000.png")]
    public void TryGetStoragePath_Rejects_Paths_Escaping_Media_Root(string? path) {
        Assert.False(MediaRootPath.TryGetStoragePath(path, "media", out var storagePath));
        Assert.Null(storagePath);
    }

    [Theory]
    [InlineData("media-root/..%5cazure-webjobs-secrets%5cmy-function-app%5chost.json")]
    [InlineData("media-root/..%5C..%5C%24logs%5Cblob%5C2026%5C11%5C17%5C0000%5C000000.log")]
    [InlineData("media-root/email-assets%5c..%5c..%5csecret.txt")]
    [InlineData("media-root/..%2fazure-webjobs-secrets%2fhost.json")]
    [InlineData("media-root/%252e%252e%255csecret.txt")]
    public async Task DownloadFile_Returns_NotFound_And_Never_Touches_Storage_For_Traversal_Attempts(string relativeUrl) {
        var fileService = new RecordingFileService();
        using var host = await CreateHost(fileService);
        using var client = host.GetTestClient();

        var response = await client.GetAsync($"/api/{relativeUrl}", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(fileService.RequestedPaths);
    }

    [Fact]
    public async Task DownloadFile_Serves_Files_Within_Media_Root() {
        var fileService = new RecordingFileService();
        using var host = await CreateHost(fileService);
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/api/media-root/email-assets/test-file-1.txt", TestContext.Current.CancellationToken);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(RecordingFileService.Content, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.All(fileService.RequestedPaths, path => Assert.Equal("media/email-assets/test-file-1.txt", path));
    }

    private static async Task<IHost> CreateHost(RecordingFileService fileService) {
        var host = new HostBuilder().ConfigureWebHost(builder => {
            builder.UseTestServer();
            builder.ConfigureServices(services => {
                services.AddRouting();
                services.AddSingleton<IFileServiceFactory>(new SingleFileServiceFactory(fileService));
                services.AddSingleton(Options.Create(new MediaApiOptions()));
            });
            builder.Configure(app => {
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGet("/api/media-root/{*path}", MediaHandlers.DownloadFile));
            });
        }).Build();
        await host.StartAsync();
        return host;
    }

    private sealed class SingleFileServiceFactory(IFileService fileService) : IFileServiceFactory
    {
        public IFileService Create(string? name) => fileService;
    }

    private sealed class RecordingFileService : IFileService
    {
        public const string Content = "This is the file contents.!";
        public List<string> RequestedPaths { get; } = [];

        public Task<byte[]> GetAsync(string filePath) {
            RequestedPaths.Add(filePath);
            return Task.FromResult(Encoding.UTF8.GetBytes(Content));
        }

        public Task<FileProperties?> GetPropertiesAsync(string filePath) {
            RequestedPaths.Add(filePath);
            return Task.FromResult<FileProperties?>(new FileProperties {
                ContentType = "text/plain",
                ETag = "\"etag\"",
                LastModified = DateTimeOffset.UtcNow,
                Length = Content.Length
            });
        }

        public Task SaveAsync(string filePath, Stream stream, FileServiceSaveOptions? saveOptions) => throw new NotSupportedException();
        public Task<IEnumerable<string>> SearchAsync(string path) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string filePath, bool isDirectory = false) => throw new NotSupportedException();
        public Task MoveAsync(string sourcePath, string destinationPath) => throw new NotSupportedException();
    }
}
