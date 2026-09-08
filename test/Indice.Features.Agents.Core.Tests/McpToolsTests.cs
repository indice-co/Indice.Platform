using Duende.AccessTokenManagement;
using Indice.Features.Agents.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Agents.Core.Tests;

public class McpToolsTests 
{
    private static readonly Lazy<IConfiguration> _configuration = new(() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> {
            ["General:Secrets:ClientId"] = "",
            ["General:Secrets:ClientSecret"] = "",
            ["General:Secrets:Scope"] = "mcp identity identity:totp cases",
            ["General:Endpoints:TokenEndpoint"] = "https://identity/connect/token",
            ["General:Endpoints:IdentityMCP"] = "https://identity/mcp",
            ["General:Endpoints:CasesMCP"] = "https://cases/mcp",
        })
        .AddUserSecrets(typeof(McpToolsTests).Assembly, optional: true)
        .Build());

    public static bool HasMcpSecrets =>
        !string.IsNullOrEmpty(_configuration.Value["General:Secrets:ClientId"]) &&
        !string.IsNullOrEmpty(_configuration.Value["General:Secrets:ClientSecret"]);

    [Fact(SkipUnless = nameof(HasMcpSecrets), Skip = "Integration test - requires valid MCP credentials")]
    public async Task TestToolRegistry() {

        var configuration = _configuration.Value;
        var services = new ServiceCollection();
        // configure dependencies
        services.AddSingleton(configuration);
        services.AddOptions();
        services.AddLogging();

        services.AddDistributedMemoryCache();
        services.AddClientCredentialsTokenManagement()
                .AddClient("mcpsecurity", credentials => {
                    // Machine-to-machine authentication (no user present, no redirect/browser).
                    credentials.TokenEndpoint = new Uri(configuration["General:Endpoints:TokenEndpoint"]!);
                    credentials.ClientId = ClientId.Parse(configuration["General:Secrets:ClientId"]!);
                    credentials.ClientSecret = ClientSecret.Parse(configuration["General:Secrets:ClientSecret"]!);
                    credentials.Scope = Scope.Parse(configuration["General:Secrets:Scope"]!);
                });
        services.AddMcpClient("id")
                .WithClientCredentialsHttpTransport(new Uri(configuration["General:Endpoints:IdentityMCP"]!), ClientCredentialsClientName.Parse("mcpsecurity"));
        services.AddMcpClient("cases")
                .WithClientCredentialsHttpTransport(new Uri(configuration["General:Endpoints:CasesMCP"]!), ClientCredentialsClientName.Parse("mcpsecurity"));

        await using var serviceProvider = services.BuildServiceProvider();

        var mcpClientFactoryIdentity = serviceProvider.GetRequiredKeyedService<IMcpClientFactory>("id");
        var mcpClientFactoryCases = serviceProvider.GetRequiredKeyedService<IMcpClientFactory>("cases");
        
        var mcpClientIdentity = await mcpClientFactoryIdentity.CreateAsync(TestContext.Current.CancellationToken);
        var mcpClientCases = await mcpClientFactoryCases.CreateAsync(TestContext.Current.CancellationToken);

        var tools = await mcpClientIdentity.ListToolsAsync(options: null, TestContext.Current.CancellationToken);

        var toolsCases = await mcpClientCases.ListToolsAsync(options: null, TestContext.Current.CancellationToken);

        Assert.NotEmpty(tools);
        Assert.NotEmpty(toolsCases);
    }
}
