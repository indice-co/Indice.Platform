using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using Indice.AspNetCore.Authorization;
using Indice.Features.Media.AspNetCore;
using Indice.Features.Media.AspNetCore.Endpoints;
using Indice.Features.Media.AspNetCore.Models;
using Indice.Features.Media.AspNetCore.Models.Requests;
using Indice.Features.Media.Data;
using Indice.Features.Messages.Tests.Security;
using Indice.Serialization;
using Indice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Indice.Features.Messages.Tests;
public class MediaLibraryTests : IAsyncLifetime
{
    // Constants
    private const string BASE_URL = "https://server/api/";
    // Private fields
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;
    private ServiceProvider _serviceProvider;

    public MediaLibraryTests(ITestOutputHelper output) {
        _output = output;
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string> {
                ["ConnectionStrings:MessagesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
                ["ConnectionStrings:StorageConnection"] = "UseDevelopmentStorage=true",
                ["General:Host"] = "https://server"
            });
        });
        builder.ConfigureServices((context, services) => {
            services.AddRouting();
            services.AddMediaLibrary(options => {
                options.PathPrefix = "/api";
                options.AcceptableFileExtensions = ".png, .jpg, .gif, .txt";
                options.Scope = Core.MessagesApi.Scope;
                options.ConfigureDbContext = (serviceProvider, dbbuilder) => dbbuilder.UseSqlServer(context.Configuration.GetConnectionString("MessagesDb"));
                options.UseFilesLocal();
            });

            services.AddAuthentication(MockAuthenticationDefaults.AuthenticationScheme)
                    .AddJwtBearer((options) => {
                        options.ForwardDefaultSelector = (httpContext) => MockAuthenticationDefaults.AuthenticationScheme;
                    })
                    .AddMock(() => DummyPrincipals.IndiceUser);
            _serviceProvider = services.BuildServiceProvider();
        });
        builder.Configure(app => {
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(e => {
                e.MapMediaLibrary();
            });
        });
        var server = new TestServer(builder);
        var handler = server.CreateHandler();
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
    }

    [Fact]
    public async Task Create_Folder_Upload_Rename_Success() {
        //Create the folders
        var rootfolderId = await CreateFolderAction("Elmai Assets");
        var level1folderId = await CreateFolderAction("Φωτογραφίες", rootfolderId);
        var level2folderId = await CreateFolderAction("Φάκελος", level1folderId);
        var root2folderId = await CreateFolderAction("Elmai Assets 2");

        // Upload the files
        _ = await PostFileAsync<UploadFileResponse>(HttpMethod.Post, "media/upload", Encoding.UTF8.GetBytes("This is the file contents.!"), "test file 1.txt", new NameValueCollection {
                { "FolderId", level2folderId.ToString() }
            });
        _ = await PostFileAsync<UploadFileResponse>(HttpMethod.Post, "media/upload", Encoding.UTF8.GetBytes("This is the file contents 2.!"), "test file 2.txt", new NameValueCollection {
                { "FolderId", level2folderId.ToString() }
            });
        _ = await PostFileAsync<UploadFileResponse>(HttpMethod.Post, "media/upload", Encoding.UTF8.GetBytes("This is the file contents 3.!"), "test file 3.txt", new NameValueCollection {
                { "FolderId", root2folderId.ToString() }
            });

        var fileService = _serviceProvider.GetRequiredKeyedService<IFileService>("Media:FileServiceKey");
        _ = await fileService.SearchAsync("media/elmai-assets");
        var file1Data = await fileService.GetAsync("media/elmai-assets/fotografies/fakelos/test-file-1.txt");
        Assert.Equal("This is the file contents.!", Encoding.UTF8.GetString(file1Data));
        // rename the folder

        await UpdateFolderAction(rootfolderId, "Email Assets");
        await Task.Delay(TimeSpan.FromMilliseconds(400));
        file1Data = await fileService.GetAsync("media/email-assets/fotografies/fakelos/test-file-1.txt");
        Assert.Equal("This is the file contents.!", Encoding.UTF8.GetString(file1Data));

        _ = await GetFolderTreeStructure();
        var content = await GetFolderContent(level2folderId);
        Assert.Equal(2, content.Files.Count);
        Assert.Equal($"{BASE_URL.TrimEnd('/')}/media-root/email-assets/fotografies/fakelos/test-file-1.txt", content.Files[0].PermaLink);

        var text = await DownloadFile(content.Files[0].PermaLink);
        Assert.Equal("This is the file contents.!", text);
    }

    [Fact]
    public void CanResolveMediaBaseHref_From_LinkGenerator() {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("server");
        httpContext.Request.Scheme = "https";
        var linkGenerator = _serviceProvider.GetRequiredService<LinkGenerator>();
        var path = linkGenerator.GetPathByRouteValues(httpContext,
                                nameof(MediaHandlers.DownloadFile),
                                values: null
                           );
        var url = linkGenerator.GetUriByName(httpContext,
                                nameof(MediaHandlers.DownloadFile),
                                values: null
                           );
        Assert.Equal("/api/media-root", path);
        Assert.Equal("https://server/api/media-root", url);
    }
    private async Task<string> DownloadFile(string url) {
        var response = await _httpClient.GetAsync(url);
        var responseText = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            _output.WriteLine(responseText);
        }

        Assert.True(response.IsSuccessStatusCode);
        return responseText;
    }


    private async Task<FolderTreeStructure> GetFolderTreeStructure() {
        var response = await _httpClient.GetAsync($"media/folders/structure");
        var responseJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            _output.WriteLine(responseJson);
        }

        Assert.True(response.IsSuccessStatusCode);
        return JsonSerializer.Deserialize<FolderTreeStructure>(responseJson, JsonSerializerOptionDefaults.GetDefaultSettings());
    }
    private async Task<FolderContent> GetFolderContent(Guid? folderId) {
        var response = await _httpClient.GetAsync($"media/folders/content?folderId={folderId}");
        var responseJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            _output.WriteLine(responseJson);
        }

        Assert.True(response.IsSuccessStatusCode);
        return JsonSerializer.Deserialize<FolderContent>(responseJson, JsonSerializerOptionDefaults.GetDefaultSettings());
    }
    private async Task UpdateFolderAction(Guid folderId, string newName) {
        var response = await _httpClient.PutAsync($"media/folders/{folderId}", new StringContent(JsonSerializer.Serialize(new UpdateFolderRequest {
            Name = newName,
        }, JsonSerializerOptionDefaults.GetDefaultSettings()), Encoding.UTF8, "application/json"));
        var responseJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            _output.WriteLine(responseJson);
        }

        Assert.True(response.IsSuccessStatusCode);
    }

    private async Task<Guid> CreateFolderAction(string name, Guid? parentFolderId = null) {
        var response = await _httpClient.PostAsync("media/folders", new StringContent(JsonSerializer.Serialize(new CreateFolderRequest {
            Name = name,
            ParentId = parentFolderId

        }, JsonSerializerOptionDefaults.GetDefaultSettings()), Encoding.UTF8, "application/json"));
        var responseJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) {
            _output.WriteLine(responseJson);
        }

        Assert.True(response.IsSuccessStatusCode);
        return JsonSerializer.Deserialize<CreateFolderResponse>(responseJson, JsonSerializerOptionDefaults.GetDefaultSettings()).FolderId;
    }

    private async Task<TResponse> PostFileAsync<TResponse>(HttpMethod method, string requestUri, byte[] fileContent, string fileName, NameValueCollection formData = null, CancellationToken cancellationToken = default) {
        using var formDataContent = new MultipartFormDataContent("upload-" + Guid.NewGuid().ToString().ToLower());
        if (fileContent != null) {
            var streamContent = new ByteArrayContent(fileContent);
            var fileExtension = Path.GetExtension(fileName);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetMimeTypeFromExtension(fileExtension));
            formDataContent.Add(streamContent, "file", fileName);
        }
        if (formData?.Count > 0) {
            var items = formData.AllKeys.SelectMany(formData.GetValues, (k, v) => new { key = k, value = v });
            foreach (var item in items) {
                formDataContent.Add(new StringContent(item.value), item.key);
            }
        }
        var httpRequest = new HttpRequestMessage(method, requestUri);
        httpRequest.Content = formDataContent;
        httpRequest.Headers.Add("Accept", "application/json");
        var httpMessage = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var content = await httpMessage.Content.ReadAsStringAsync();
        TResponse? response;
        if (httpMessage.IsSuccessStatusCode) {
            response = JsonSerializer.Deserialize<TResponse>(content, JsonSerializerOptionDefaults.GetDefaultSettings());
        } else {
            throw new Exception(content);
        }
        return response;
    }

    private static string GetMimeTypeFromExtension(string extension) {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".htm"] = "text/html",
            [".html"] = "text/html",
            [".jpg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".svg"] = "image/svg+xml",
            [".txt"] = "text/plain",
        };
        return mappings.ContainsKey(extension) ? mappings[extension] : string.Empty;
    }

    public async Task InitializeAsync() {
        var db = _serviceProvider.GetRequiredService<MediaDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() {
        var db = _serviceProvider.GetRequiredService<MediaDbContext>();
        await db.Database.EnsureDeletedAsync();
        await _serviceProvider.DisposeAsync();
    }
}