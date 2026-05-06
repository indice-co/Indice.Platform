#if NET9_0_OR_GREATER
using System.Net.Http.Json;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Indice.Serialization;
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
    private readonly IHost _host;
    private readonly ITestOutputHelper _output;
    private HttpClient _httpClient = null!;
    private ServiceProvider _serviceProvider = null!;
    public static Action<JsonSerializerOptions> ConfigureIndiceHttpJsonOptions = (options) => {
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Converters.Add(new JsonStringDecimalConverter());
        options.Converters.Add(new JsonStringDoubleConverter());
        options.Converters.Add(new JsonStringInt32Converter());
        options.Converters.Add(new JsonStringBooleanConverter());
        options.Converters.Add(new JsonAnyStringConverter());
        options.Converters.Add(new TypeConverterJsonAdapterFactory());
    };

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
                    services.ConfigureHttpJsonOptions(options => {
                        ConfigureIndiceHttpJsonOptions(options.SerializerOptions);
                    });
                    services.AddOpenApi("tests", options => options.AddDocumentInfo().ControllerActionAsOperationId());
                    services.AddOpenApi("nullables", options => options.AddDocumentInfo().ControllerActionAsOperationId());
                    services.AddOpenApi("ignore-openapi", options => options.AddDocumentInfo().ControllerActionAsOperationId());
                    services.AddEndpointsApiExplorer();
                    services.AddControllers().ConfigureApplicationPartManager(m => m.FeatureProviders.Add(new OpenApiTestFeatureProvider()));
                });
                webBuilder.Configure(app => {
                    app.UseRouting();
                    app.UseEndpoints(e => {
                        e.MapTestEndpoints();
                        e.MapNullableTestEndpoints();
                        e.MapIgnoreAttributeEndpoints();
                        e.MapControllers();
                        e.MapOpenApi();
                    });
                });
                webBuilder.UseTestServer();
            });
        _host = builder.Build();
    }

    public async Task DisposeAsync() {
        await _serviceProvider.DisposeAsync();
        await ((IAsyncDisposable)_host).DisposeAsync();
    }

    public async Task InitializeAsync() {
        await _host.StartAsync();
        var server = _host.GetTestServer();
        var handler = server.CreateHandler();
        _serviceProvider = (ServiceProvider)_host.Services;
        _httpClient = new HttpClient(handler) {
            BaseAddress = new Uri(BASE_URL)
        };
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
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ConfigureIndiceHttpJsonOptions(jsonOptions);
        var menu = await response.Content.ReadFromJsonAsync<List<MenuItem>>(jsonOptions);

        Assert.NotEmpty(menu!);
        var openApi = await _httpClient.GetStringAsync("openapi/tests.json");
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


        var longListTypeSchema = json!["components"]!["schemas"]!["LongListType"];
        Assert.NotNull(longListTypeSchema);
        var longListTypeAsStringSchema = json!["components"]!["schemas"]!["LongListTypeAsString"];
        Assert.NotNull(longListTypeAsStringSchema);
    }
    [Fact]
    public async Task OpenApiHandlesNullableEnumsModels() {
        var openApi = await _httpClient.GetStringAsync("openapi/nullables.json");
        Assert.NotEmpty(openApi);

        var json = JsonNode.Parse(openApi);
        var actualSchema = json!["components"]!["schemas"]!["NullableEnumsTestRequest"];
        var expectedSchema = "{\"type\":\"object\",\"properties\":{\"nullableType\":{\"allOf\":[{\"$ref\":\"#/components/schemas/NullableEnumsType\"}],\"additionalProperties\":false,\"nullable\":true}},\"additionalProperties\":false}";
        Assert.Equal(expectedSchema, actualSchema!.ToJsonString());
        var actualEnumSchema = json!["components"]!["schemas"]!["NullableEnumsType"];
        var expectedEnumSchema = "{\"enum\":[0,1,2,3],\"type\":\"integer\",\"x-enum-varnames\":[\"Valid\",\"Invalid\",\"Draft\",\"Deleted\"]}";
        Assert.Equal(expectedEnumSchema, actualEnumSchema!.ToJsonString());
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
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ConfigureIndiceHttpJsonOptions(jsonOptions);
        var menu = await response.Content.ReadFromJsonAsync<List<MenuItem>>(jsonOptions);

        Assert.NotEmpty(menu!);
        var openApi = await _httpClient.GetStringAsync("openapi/tests.json");
        Assert.NotEmpty(openApi);

        var json = JsonNode.Parse(openApi);
        var menuItemSchema = json!["components"]!["schemas"]!["MenuItem"];
        var expectedMenuItemSchema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"},\"description\":{\"type\":\"string\"},\"type\":{\"$ref\":\"#/components/schemas/MenuType\"},\"children\":{\"type\":\"array\",\"items\":{\"$ref\":\"#/components/schemas/MenuItem\"}}},\"additionalProperties\":false}";
        Assert.Equal(expectedMenuItemSchema, menuItemSchema!.ToJsonString());

        var uploadRequestSchema = json!["components"]!["schemas"]!["UploadFileRequest"];
        // IFormFile is by default renamed to FileParam in the schema transformer MappedTypesTransformer
        var expectedUploadRequestSchema = "{\"type\":\"object\",\"properties\":{\"file\":{\"oneOf\":[{\"type\":\"null\"},{\"$ref\":\"#/components/schemas/FileParam\"}]},\"name\":{\"type\":\"string\"},\"description\":{\"type\":[\"null\",\"string\"]}},\"additionalProperties\":false}";
        Assert.Equal(expectedUploadRequestSchema, uploadRequestSchema!.ToJsonString());

        var sampleEnumSchema = json!["components"]!["schemas"]!["SampleEnum"];
        Assert.NotNull(sampleEnumSchema);
        var expectedSampleEnumSchema = "{\"enum\":[1,2,3],\"type\":\"integer\",\"x-enum-varnames\":[\"Value1\",\"Value2\",\"Value3\"]}";
        Assert.Equal(expectedSampleEnumSchema, sampleEnumSchema.ToJsonString());

        var mvcOperationId = json!["paths"]!["/mvc/menu"]!["get"]!["operationId"]!.ToString();
        Assert.Equal("OpenApiTests_GetMenuItems", mvcOperationId!);

        var parameterEnumSchema = json!["paths"]!["/mvc/menu"]!["get"]!["parameters"]![0]!["schema"];
        Assert.Equal("{\"$ref\":\"#/components/schemas/SampleEnum\"}", parameterEnumSchema!.ToJsonString());

        var longListTypeSchema = json!["components"]!["schemas"]!["LongListType"];
        Assert.NotNull(longListTypeSchema);
        var longListTypeAsStringSchema = json!["components"]!["schemas"]!["LongListTypeAsString"];
        Assert.NotNull(longListTypeAsStringSchema);
    }

    [Fact]
    public async Task OpenApiHandlesNullableEnumsModels() {
        var openApi = await _httpClient.GetStringAsync("openapi/nullables.json");
        Assert.NotEmpty(openApi);

        var json = JsonNode.Parse(openApi);
        var actualSchema = json!["components"]!["schemas"]!["NullableEnumsTestRequest"];
        var expectedSchema = "{\"type\":\"object\",\"properties\":{\"nullableType\":{\"oneOf\":[{\"type\":\"null\"},{\"$ref\":\"#/components/schemas/NullableEnumsType\"}]}},\"additionalProperties\":false}";
        Assert.Equal(expectedSchema, actualSchema!.ToJsonString());
        var actualEnumSchema = json!["components"]!["schemas"]!["NullableEnumsType"];
        var expectedEnumSchema = "{\"enum\":[0,1,2,3],\"type\":\"integer\",\"x-enum-varnames\":[\"Valid\",\"Invalid\",\"Draft\",\"Deleted\"]}";
        Assert.Equal(expectedEnumSchema, actualEnumSchema!.ToJsonString());
    }

    [Fact]
    public async Task OpenApiHandleOpenApiIgnoreAttribute() {
        var openApi = await _httpClient.GetStringAsync("openapi/ignore-openapi.json");
        Assert.NotEmpty(openApi);

        var json = JsonNode.Parse(openApi);
        var schema = json!["components"]!["schemas"]!["IgnoreAttributeResponse"];
        var expectedSchema = "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":[\"null\",\"string\"]},\"nest\":{\"oneOf\":[{\"type\":\"null\"},{\"$ref\":\"#/components/schemas/IgnoreAttrubuteNest\"}]}},\"additionalProperties\":false}";

        Assert.Equal(expectedSchema, schema!.ToJsonString());
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

    public class PrimitivesTestRequest
    {
        public decimal? Money { get; set; }
        public string? Currency { get; set; }
        public int Year { get; set; }
        public long Id { get; set; }
        public DateTime? CreatedDate { get; set; }
        public List<string> Tags { get; set; } = [];
        public List<double> RandomNumbers { get; set; } = [];
        public List<decimal> RandomDecimals { get; set; } = [];
        public List<int> RandomIntegers { get; set; } = [];
        public List<MenuType> MenuTypes { get; set; } = [];
        public List<DateTime> Schedule { get; set; } = [];
        public Dictionary<string, string> Mappings { get; set; } = [];
    }

    public class NullableEnumsTestRequest
    {
        public NullableEnumsType? NullableType { get; set; }
    }
    public enum NullableEnumsType
    {
        Valid,
        Invalid,
        Draft,
        Deleted,

    }
    public class LongListRequest
    {
        public LongListType LongList { get; set; } = LongListType.None;
        public LongListTypeAsString LongListText { get; set; } = LongListTypeAsString.None;
    }
    [Flags]
    public enum LongListType : long
    {
        None = 0,
        A = 1L << 0,    // 1
        B = 1L << 1,    // 2
        C = 1L << 2,    // 4
        D = 1L << 3,    // 8
        E = 1L << 4,    // 16
        F = 1L << 5,    // 32
        G = 1L << 6,    // 64
        H = 1L << 7,    // 128
        I = 1L << 8,    // 256
        J = 1L << 9,    // 512
        K = 1L << 10,   // 1024
        L = 1L << 11,   // 2048
        M = 1L << 12,   // 4096
        N = 1L << 13,   // 8192
        O = 1L << 14,   // 16384
        P = 1L << 15,   // 32768
        Q = 1L << 16,   // 65536
        R = 1L << 17,   // 131072
        S = 1L << 18,   // 262144
        T = 1L << 19,   // 524288
        U = 1L << 20,   // 1048576
        V = 1L << 21,   // 2097152
        W = 1L << 22,   // 4194304
        X = 1L << 23,   // 8388608
        Y = 1L << 24,   // 16777216
        Z = 1L << 25,   // 33554432
        AA = 1L << 26,  // 67108864
        AB = 1L << 27,  // 134217728
        AC = 1L << 28,  // 268435456
        AD = 1L << 29,  // 536870912
        AE = 1L << 30,  // 1073741824
        AF = 1L << 31,  // 2147483648
        AG = 1L << 32,  // 4294967296
        AH = 1L << 33,  // 8589934592
        AI = 1L << 34,  // 17179869184
        AJ = 1L << 35,  // 34359738368
        AK = 1L << 36   // 68719476736
    }


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LongListTypeAsString : long
    {
        None = 0,
        A = 1L << 0,    // 1
        B = 1L << 1,    // 2
        C = 1L << 2,    // 4
        D = 1L << 3,    // 8
        E = 1L << 4,    // 16
        F = 1L << 5,    // 32
        G = 1L << 6,    // 64
        H = 1L << 7,    // 128
        I = 1L << 8,    // 256
        J = 1L << 9,    // 512
        K = 1L << 10,   // 1024
        L = 1L << 11,   // 2048
        M = 1L << 12,   // 4096
        N = 1L << 13,   // 8192
        O = 1L << 14,   // 16384
        P = 1L << 15,   // 32768
        Q = 1L << 16,   // 65536
        R = 1L << 17,   // 131072
        S = 1L << 18,   // 262144
        T = 1L << 19,   // 524288
        U = 1L << 20,   // 1048576
        V = 1L << 21,   // 2097152
        W = 1L << 22,   // 4194304
        X = 1L << 23,   // 8388608
        Y = 1L << 24,   // 16777216
        Z = 1L << 25,   // 33554432
        AA = 1L << 26,  // 67108864
        AB = 1L << 27,  // 134217728
        AC = 1L << 28,  // 268435456
        AD = 1L << 29,  // 536870912
        AE = 1L << 30,  // 1073741824
        AF = 1L << 31,  // 2147483648
        AG = 1L << 32,  // 4294967296
        AH = 1L << 33,  // 8589934592
        AI = 1L << 34,  // 17179869184
        AJ = 1L << 35,  // 34359738368
        AK = 1L << 36   // 68719476736
    }

    public class IgnoreAttributeResponse
    {
        public string? Id { get; set; }

#if NET10_0_OR_GREATER
        [OpenApi.Attributes.OpenApiIgnore]
#endif
        public int MyProperty { get; set; }

#if NET10_0_OR_GREATER
        [OpenApi.Attributes.OpenApiIgnore]
#endif
        public required string IgnoredRequiredProperty { get; set; }

        public IgnoreAttrubuteNest? Nest { get; set; }
    }

    public class IgnoreAttrubuteNest
    {
        public string NestedId { get; set; } = null!;

#if NET10_0_OR_GREATER
        [OpenApi.Attributes.OpenApiIgnore]
#endif
        public bool NestedAndIngored { get; set; }
    }

}

[ApiController]
[ApiExplorerSettings(GroupName = "tests")]
public class OpenApiTestsController
{
    [HttpGet("/mvc/menu")]
    public IActionResult GetMenuItems([FromQuery] SampleFilterRequest filter) {
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
        group.WithGroupName("tests");
        group.WithTags("Tests");
        group.MapGet("menu", GetMenuItems)
             .WithName(nameof(GetMenuItems));
        group.MapPost("upload", UploadAttachment)
             .WithName(nameof(UploadAttachment))
             .Accepts<UploadFileRequest>(MediaTypeNames.Multipart.FormData);
        group.MapPost("converters", UpdateWithConverters)
             .WithName(nameof(UpdateWithConverters));
        group.MapPost("long-enum", UpdateLongTypeEnum)
             .WithName(nameof(UpdateLongTypeEnum));

        return routes;
    }
    public static IEndpointRouteBuilder MapNullableTestEndpoints(this IEndpointRouteBuilder routes) {
        var group = routes.MapGroup("tests");
        group.WithGroupName("nullables");
        group.WithTags("Tests");
        group.MapPost("nullable-enum/{parentId}", PostNullableEnum)
             .WithName(nameof(PostNullableEnum));

        return routes;
    }

    public static IEndpointRouteBuilder MapIgnoreAttributeEndpoints(this IEndpointRouteBuilder routes) {
        var group = routes.MapGroup("ignore-openapi");
        group.WithGroupName("ignore-openapi");
        group.WithTags("Ignore OpenApi");
        group.MapGet("ignore-open-api/sample", GetIgnoreAttributeResponse)
                   .WithName(nameof(GetIgnoreAttributeResponse));

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
    public static NoContent UpdateWithConverters(PrimitivesTestRequest request) {
        return TypedResults.NoContent();
    }
    public static NoContent UpdateLongTypeEnum(LongListRequest request) {
        return TypedResults.NoContent();
    }
    public static NoContent PostNullableEnum(string parentId, NullableEnumsTestRequest request) {
        return TypedResults.NoContent();
    }

    public static Ok<AttachmentLink> UploadAttachment(UploadFileRequest uploadFileRequest) {
        return TypedResults.Ok(new AttachmentLink {
            AttachmentId = Guid.Parse("1b62a5f3-f2d2-43be-81f9-572e97862b60")
        });
    }

    public static Ok<IgnoreAttributeResponse> GetIgnoreAttributeResponse() {
        return TypedResults.Ok(new IgnoreAttributeResponse {
            Id = "123",
            MyProperty = 456,
            IgnoredRequiredProperty = "This should be ignored in the OpenAPI schema",
            Nest = new IgnoreAttrubuteNest {
                NestedId = "Nested123",
                NestedAndIngored = true
            }
        });
    }
}

#endif