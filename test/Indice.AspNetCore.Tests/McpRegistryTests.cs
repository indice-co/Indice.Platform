using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Tests;

public class McpRegistryTests
{
    private static readonly Dictionary<string, string?> DefaultConfiguration = new() {
        ["McpRegistry:DefaultPageSize"] = "2",
        ["McpRegistry:Servers:0:Name"] = "io.github.indice/weather",
        ["McpRegistry:Servers:0:Description"] = "Weather forecasts",
        ["McpRegistry:Servers:0:Version"] = "1.0.0",
        ["McpRegistry:Servers:0:Remotes:0:Url"] = "https://example.com/weather/v1/mcp",
        ["McpRegistry:Servers:1:Name"] = "io.github.indice/weather",
        ["McpRegistry:Servers:1:Description"] = "Weather forecasts",
        ["McpRegistry:Servers:1:Version"] = "1.10.0",
        ["McpRegistry:Servers:1:Remotes:0:Url"] = "https://example.com/weather/mcp",
        ["McpRegistry:Servers:1:Remotes:0:Headers:0:Name"] = "Authorization",
        ["McpRegistry:Servers:1:Remotes:0:Headers:0:IsSecret"] = "true",
        ["McpRegistry:Servers:2:Name"] = "io.github.indice/weather",
        ["McpRegistry:Servers:2:Description"] = "Weather forecasts",
        ["McpRegistry:Servers:2:Version"] = "1.10.0-rc.1",
        ["McpRegistry:Servers:2:Remotes:0:Url"] = "https://example.com/weather/beta/mcp",
        ["McpRegistry:Servers:3:Name"] = "io.github.indice/filesystem",
        ["McpRegistry:Servers:3:Description"] = "Filesystem",
        ["McpRegistry:Servers:3:Version"] = "0.1.0",
        ["McpRegistry:Servers:3:Packages:0:RegistryType"] = "npm",
        ["McpRegistry:Servers:3:Packages:0:Identifier"] = "@modelcontextprotocol/server-filesystem",
        ["McpRegistry:Servers:3:Packages:0:Version"] = "0.1.0",
        ["McpRegistry:Servers:3:Packages:0:Transport:Type"] = "stdio",
        ["McpRegistry:Servers:4:Name"] = "io.github.indice/legacy",
        ["McpRegistry:Servers:4:Description"] = "Legacy",
        ["McpRegistry:Servers:4:Version"] = "1.0.0",
        ["McpRegistry:Servers:4:Status"] = "Deleted",
        ["McpRegistry:Servers:4:Remotes:0:Url"] = "https://example.com/legacy/mcp",
    };

    private static async Task<IHost> CreateHost(Dictionary<string, string?>? configuration = null) {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(configuration ?? DefaultConfiguration);
        builder.AddMcpRegistry();
        var app = builder.Build();
        app.UseCors();
        app.MapMcpRegistry();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static async Task<JsonElement> GetJson(HttpClient client, string url, HttpStatusCode expectedStatus = HttpStatusCode.OK) {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Origin", "https://github.com");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return JsonDocument.Parse(content).RootElement;
    }

    [Fact]
    public async Task ListServers_ReturnsPagedNonDeletedServers() {
        using var host = await CreateHost();
        var client = host.GetTestClient();

        var page1 = await GetJson(client, "/v0.1/servers");
        Assert.Equal(2, page1.GetProperty("metadata").GetProperty("count").GetInt32());
        var cursor = page1.GetProperty("metadata").GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrEmpty(cursor));

        var page2 = await GetJson(client, $"/v0.1/servers?cursor={cursor}");
        Assert.Equal(2, page2.GetProperty("metadata").GetProperty("count").GetInt32());
        Assert.False(page2.GetProperty("metadata").TryGetProperty("nextCursor", out _));

        var names = page1.GetProperty("servers").EnumerateArray().Concat(page2.GetProperty("servers").EnumerateArray())
                         .Select(x => x.GetProperty("server").GetProperty("name").GetString()).ToList();
        Assert.DoesNotContain("io.github.indice/legacy", names);
        Assert.Equal(4, names.Count);
    }

    [Fact]
    public async Task ListServers_FiltersLatestVersionAndSearch() {
        using var host = await CreateHost();
        var client = host.GetTestClient();

        var result = await GetJson(client, "/v0.1/servers?version=latest&search=weather");
        var servers = result.GetProperty("servers").EnumerateArray().ToList();
        Assert.Single(servers);
        Assert.Equal("1.10.0", servers[0].GetProperty("server").GetProperty("version").GetString());
        var official = servers[0].GetProperty("_meta").GetProperty("io.modelcontextprotocol.registry/official");
        Assert.True(official.GetProperty("isLatest").GetBoolean());
        Assert.Equal("active", official.GetProperty("status").GetString());

        var withDeleted = await GetJson(client, "/v0.1/servers?include_deleted=true&limit=100");
        Assert.Equal(5, withDeleted.GetProperty("servers").GetArrayLength());
    }

    [Theory]
    [InlineData("/v0.1/servers/io.github.indice%2Fweather/versions/latest", "1.10.0")]
    [InlineData("/v0.1/servers/io.github.indice/weather/versions/latest", "1.10.0")]
    [InlineData("/v0.1/servers/io.github.indice%2Fweather/versions/1.10.0-rc.1", "1.10.0-rc.1")]
    [InlineData("/v0.1/servers/io.github.indice%2Fweather/versions/1.0.0", "1.0.0")]
    public async Task GetServerVersion_ReturnsServer(string url, string expectedVersion) {
        using var host = await CreateHost();
        var client = host.GetTestClient();

        var result = await GetJson(client, url);
        var server = result.GetProperty("server");
        Assert.Equal("io.github.indice/weather", server.GetProperty("name").GetString());
        Assert.Equal(expectedVersion, server.GetProperty("version").GetString());
        Assert.Equal("streamable-http", server.GetProperty("remotes")[0].GetProperty("type").GetString());
    }

    [Theory]
    [InlineData("/v0.1/servers/io.github.indice%2Funknown/versions/latest")]
    [InlineData("/v0.1/servers/io.github.indice%2Fweather/versions/9.9.9")]
    [InlineData("/v0.1/servers/io.github.indice%2Flegacy/versions/latest")]
    public async Task GetServerVersion_ReturnsNotFound(string url) {
        using var host = await CreateHost();
        var client = host.GetTestClient();

        var result = await GetJson(client, url, HttpStatusCode.NotFound);
        Assert.False(string.IsNullOrEmpty(result.GetProperty("error").GetString()));
    }

    [Fact]
    public async Task ListServerVersions_ReturnsVersionsNewestFirst() {
        using var host = await CreateHost();
        var client = host.GetTestClient();

        var result = await GetJson(client, "/v0.1/servers/io.github.indice%2Fweather/versions");
        var versions = result.GetProperty("servers").EnumerateArray().Select(x => x.GetProperty("server").GetProperty("version").GetString()).ToList();
        Assert.Equal(["1.10.0", "1.10.0-rc.1", "1.0.0"], versions);
    }

    [Fact]
    public async Task Preflight_ReturnsCorsHeaders() {
        using var host = await CreateHost();
        var client = host.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/v0.1/servers/io.github.indice%2Fweather/versions/latest");
        request.Headers.Add("Origin", "https://github.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("GET,OPTIONS", response.Headers.GetValues("Access-Control-Allow-Methods").Single());
        Assert.Equal("Authorization,Content-Type", response.Headers.GetValues("Access-Control-Allow-Headers").Single());
    }

    [Fact]
    public async Task InvalidConfiguration_FailsOnStart() {
        var configuration = new Dictionary<string, string?> {
            ["McpRegistry:Servers:0:Name"] = "invalid-name",
            ["McpRegistry:Servers:0:Description"] = "Invalid",
            ["McpRegistry:Servers:0:Version"] = "latest",
        };
        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => CreateHost(configuration));
        Assert.Equal(2, exception.Failures.Count());
    }
}
