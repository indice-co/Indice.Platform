#if NET9_0_OR_GREATER
using System.Net.Http.Json;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder => {
                webBuilder.ConfigureAppConfiguration(builder => {
                    builder.AddInMemoryCollection(new Dictionary<string, string?> {
                        ["ConnectionStrings:MessagesDb"] = $"Server=(localdb)\\MSSQLLocalDB;Database=MessagesDb.Test_{Environment.Version.Major}_{Guid.NewGuid()};Trusted_Connection=True;MultipleActiveResultSets=true",
                        ["ConnectionStrings:StorageConnection"] = "UseDevelopmentStorage=true",
                        ["General:Host"] = "https://server"
                    });
                });
                webBuilder.ConfigureServices((context, services) => {
                    services.AddRouting();
                    services.AddOpenApi(options => options.AddDocumentInfo().ControllerActionAsOperationId());
                    services.AddEndpointsApiExplorer();
                    services.AddControllers().ConfigureApplicationPartManager(m => m.FeatureProviders.Add(new OpenApiTestFeatureProvider()));
                });
                webBuilder.Configure(app => {
                    app.UseRouting();
                    app.UseEndpoints(e => {
                        e.MapTestEndpoints();
                        e.MapControllers();
                        e.MapOpenApi();
                    });
                });
                webBuilder.UseTestServer();
            });
        var host = builder.Build();
        var server = host.GetTestServer();
        var handler = server.CreateHandler();
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
        _serviceProvider = (ServiceProvider)host.Services;
    }

    public async Task DisposeAsync() {
        await _serviceProvider.DisposeAsync();
    }

    public Task InitializeAsync() {
        return Task.CompletedTask;
    }
#if NET9_0
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
        var expectedMenuItemSchema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"},\"description\":{\"type\":\"string\"},\"type\":{\"$ref\":\"#/components/schemas/MenuType\"},\"children\":{\"type\":\"array\",\"items\":{\"$ref\":\"#/components/schemas/MenuItem\"}}},\"additionalProperties\":false}";
        Assert.Equal(expectedMenuItemSchema, menuItemSchema!.ToJsonString());

        var uploadRequestSchema = json!["components"]!["schemas"]!["UploadFileRequest"];
        var expectedUploadRequestSchema = "{\"type\":\"object\",\"properties\":{\"file\":{\"type\":\"string\",\"format\":\"binary\",\"nullable\":true},\"name\":{\"type\":\"string\"},\"description\":{\"type\":\"string\",\"nullable\":true}},\"additionalProperties\":false}";
        Assert.Equal(expectedUploadRequestSchema, uploadRequestSchema!.ToJsonString());

        var sampleEnumSchema = json!["components"]!["schemas"]!["SampleEnum"];
        Assert.Null(sampleEnumSchema);

        var mvcOperationId = json!["paths"]!["/mvc/menu"]!["get"]!["operationId"]!.ToString();
        Assert.Equal("OpenApiTests_GetMenuItems", mvcOperationId!);

        var parameterEnumSchema = json!["paths"]!["/mvc/menu"]!["get"]!["parameters"]![0]!["schema"];
        var expectedSampleEnumSchema = "{\"enum\":[1,2,3],\"type\":\"integer\",\"x-enum-varnames\":[\"Value1\",\"Value2\",\"Value3\"]}";
        Assert.Equal(expectedSampleEnumSchema, parameterEnumSchema!.ToJsonString());
    }
}
#else
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
        var expectedMenuItemSchema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"},\"description\":{\"type\":\"string\"},\"type\":{\"$ref\":\"#/components/schemas/MenuType\"},\"children\":{\"type\":\"array\",\"items\":{\"$ref\":\"#/components/schemas/MenuItem\"}}},\"additionalProperties\":false}";
        Assert.Equal(expectedMenuItemSchema, menuItemSchema!.ToJsonString());

        var uploadRequestSchema = json!["components"]!["schemas"]!["UploadFileRequest"];
        ///TODO: check this for dotnet 10 it should probably be same or similar to the dotnet9 one 
        ///      using the default mappings of the MappedTypeTransformer 
        ///      for IFormFile to be consistent with the rest of the framework. 
        var expectedUploadRequestSchema = "{\"type\":\"object\",\"properties\":{\"file\":{\"oneOf\":[{\"type\":\"null\"},{\"$ref\":\"#/components/schemas/IFormFile\"}]},\"name\":{\"type\":\"string\"},\"description\":{\"type\":\"string\"}},\"additionalProperties\":false}";
        Assert.Equal(expectedUploadRequestSchema, uploadRequestSchema!.ToJsonString());

        var sampleEnumSchema = json!["components"]!["schemas"]!["SampleEnum"];
        Assert.NotNull(sampleEnumSchema);
        var expectedSampleEnumSchema = "{\"enum\":[1,2,3],\"type\":\"integer\",\"x-enum-varnames\":[\"Value1\",\"Value2\",\"Value3\"]}";
        Assert.Equal(expectedSampleEnumSchema, sampleEnumSchema.ToJsonString());

        var mvcOperationId = json!["paths"]!["/mvc/menu"]!["get"]!["operationId"]!.ToString();
        Assert.Equal("OpenApiTests_GetMenuItems", mvcOperationId!);

        var parameterEnumSchema = json!["paths"]!["/mvc/menu"]!["get"]!["parameters"]![0]!["schema"];
        Assert.Equal("{\"$ref\":\"#/components/schemas/SampleEnum\"}", parameterEnumSchema!.ToJsonString());
    }
}
#endif

public class OpenApiTestsModels
{
    public class MenuItem
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MenuType Type { get; set; } = MenuType.Link;
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

    public enum SampleEnum
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 3
    }
    public enum MenuType
    {
        Link = 1,
        Category = 2,
    }

    public class SampleFilterRequest
    {
        public SampleEnum? EnumValue { get; set; }
    }
}

[ApiController]
public class OpenApiTestsController
{
    [HttpGet("/mvc/menu")]
    public IActionResult GetMenuItems([FromQuery]SampleFilterRequest filter) {
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
        return new OkObjectResult(items);
    }
}

public class OpenApiTestFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    /// <summary>Populates the feature for the current ASP.NET app.</summary>
    /// <param name="parts">The list of <see cref="ApplicationPart"/> instances in the application.</param>
    /// <param name="feature">The feature instance to populate.</param>
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature) {
        var type = typeof(OpenApiTestsController).GetTypeInfo();
        if (!feature.Controllers.Any(x => x == type)) {
            feature.Controllers.Add(type);
        }
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