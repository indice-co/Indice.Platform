#if NET9_0_OR_GREATER
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using static Indice.AspNetCore.Tests.OpenApiTestsModels;

namespace Indice.AspNetCore.Tests;

public class OpenApiTests : IAsyncLifetime
{
    // Constants
    private const string BASE_URL = "https://server";
    // Private fields
    private readonly HttpClient _httpClient;
    private readonly ITestOutputHelper _output;
    private ServiceProvider _serviceProvider;

    public OpenApiTests(ITestOutputHelper output) {
        _output = output;
        var builder = new WebHostBuilder();
        builder.ConfigureAppConfiguration(builder => {
            builder.AddInMemoryCollection(new Dictionary<string, string?> {
                ["ConnectionStrings:MessagesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
                ["ConnectionStrings:StorageConnection"] = "UseDevelopmentStorage=true",
                ["General:Host"] = "https://server"
            });
        });
        builder.ConfigureServices((context, services) => {
            services.AddRouting();
            services.AddOpenApi(options => options.AddDocumentInfo());
            services.AddEndpointsApiExplorer();
        });
        builder.Configure(app => {
            app.UseRouting();
            app.UseEndpoints(e => {
                e.MapTestEndpoints();
                e.MapOpenApi();
            });
        });
        var server = new TestServer(builder);
        var handler = server.CreateHandler();
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
        _serviceProvider = (ServiceProvider)server.Services;
    }

    public async Task DisposeAsync() {
        await _serviceProvider.DisposeAsync();
    }

    public Task InitializeAsync() {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task OpenApiHandlesRecursiveModels() {
        // Act
        var response = await _httpClient.GetAsync("tests/menu");
        if (!response.IsSuccessStatusCode) {
            _output.WriteLine(await response.Content.ReadAsStringAsync());
        }
        Assert.True(response.IsSuccessStatusCode);
        var menu = await response.Content.ReadFromJsonAsync<List<MenuItem>>();
        Assert.NotEmpty(menu!);
        var openApi = await _httpClient.GetStringAsync("openapi/v1.json");
        Assert.NotEmpty(openApi);
        var json = JsonNode.Parse(openApi);
        var menuItemSchema = json!["components"]!["schemas"]!["MenuItem"];
        var uploadRequestSchema = json!["components"]!["schemas"]!["UploadFileRequest"];
    }
}

public class OpenApiTestsModels
{
    public class MenuItem
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<MenuItem> Children { get; set; } = [];
    }

    public class UploadFileRequest
    {
        public IFormFile? File { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

    }
    public class AttachmentLink
    {
        public Guid AttachmentId { get; set; }

    }
}

public static class OpenApiTestsEndpoints
{
    public static IEndpointRouteBuilder MapTestEndpoints(this IEndpointRouteBuilder routes) {
        var group = routes.MapGroup("tests");
        group.WithTags("Tests");
        group.MapGet("menu", GetMenuItems)
             .WithName(nameof(GetMenuItems));
        group.MapPost("upload", UploadAttachment)
             .WithName(nameof(UploadAttachment))
             .Accepts<UploadFileRequest>(MediaTypeNames.Multipart.FormData);

        return routes;
    }

    public static Ok<List<MenuItem>> GetMenuItems() {
        var items = new List<MenuItem>
        {
                new()
                {
                    Name = "Home",
                    Description = "Go to home page",
                    Children = [
                        new() { Name = "Sub Home 1", Description = "Sub Home 1 Description" },
                        new() { Name = "Sub Home 2", Description = "Sub Home 2 Description" }
                    ]
                },
                new()
                {
                    Name = "About",
                    Description = "Learn more about us"
                }
            };
        return TypedResults.Ok(items);
    }

    public static Ok<AttachmentLink> UploadAttachment(UploadFileRequest uploadFileRequest) {
        return TypedResults.Ok(new AttachmentLink {
            AttachmentId = Guid.Parse("1b62a5f3-f2d2-43be-81f9-572e97862b60")
        });
    }
}

#endif