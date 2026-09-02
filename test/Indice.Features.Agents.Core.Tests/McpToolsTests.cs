using Duende.AccessTokenManagement;
using Indice.Features.Agents.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Agents.Core.Tests;

public class McpToolsTests 
{

    [Fact(Skip = "Integration test - requires valid MCP credentials")]
    public async Task TestToolRegistry() {

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["General:Secrets:ClientId"] = "xxxx",
            ["General:Secrets:ClientSecret"] = "xxxxxxxxxxxxxx",
            ["General:Secrets:Scope"] = "mcp identity identity:totp cases",
            ["General:Endpoints:TokenEndpoint"] = "https://sampleid/connect/token",
            ["General:Endpoints:IdentityMCP"] = "https://sampleservice1/mcp",
            ["General:Endpoints:CasesMCP"] = "https://sampleservice2/mcp",
        }).Build();
        var services = new ServiceCollection();
        // configure dependencies
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddMcpClient("id")
                .WithClientCredentialsHttpTransport(new Uri(configuration["General:Endpoints:IdentityMCP"]!), credentials => {
                    // Machine-to-machine authentication (no user present, no redirect/browser).
                    credentials.TokenEndpoint = new Uri(configuration["General:Endpoints:TokenEndpoint"]!);
                    credentials.ClientId = ClientId.Parse(configuration["General:Secrets:ClientId"]!);
                    credentials.ClientSecret = ClientSecret.Parse(configuration["General:Secrets:ClientSecret"]!);
                    credentials.Scope = Scope.Parse(configuration["General:Secrets:Scope"]!);
                });
        services.AddMcpClient("cases")
                .WithClientCredentialsHttpTransport(new Uri(configuration["General:Endpoints:CasesMCP"]!), ClientCredentialsClientName.Parse("mcp-id-auth"));

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
