using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Providers;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Agents.FreeLLM.Tests;

/// <summary>
/// Round trips against a real OpenAI-compatible endpoint (a FreeLLM gateway, Ollama, OpenRouter...). Skipped unless the endpoint is configured in the
/// user secrets of this project, e.g.:
/// <code>
/// dotnet user-secrets set "Dex:FreeLLM:Endpoint" "http://localhost:11434/v1" --project test/Indice.Features.Agents.FreeLLM.Tests
/// dotnet user-secrets set "Dex:FreeLLM:Models:Reasoning" "llama3.1"
/// dotnet user-secrets set "Dex:FreeLLM:Models:Embedding" "a-1536-dimension-embedding-model"
/// </code>
/// </summary>
public class LiveEndpointTests
{
    private static readonly Lazy<IConfiguration> _configuration = new(() => new ConfigurationBuilder()
        .AddUserSecrets(typeof(LiveEndpointTests).Assembly, optional: true)
        .Build());

    public static bool HasChatEndpoint =>
        !string.IsNullOrEmpty(_configuration.Value["Dex:FreeLLM:Endpoint"]) && !string.IsNullOrEmpty(_configuration.Value["Dex:FreeLLM:Models:Reasoning"]);

    public static bool HasEmbeddingEndpoint =>
        !string.IsNullOrEmpty(_configuration.Value["Dex:FreeLLM:Models:Embedding"]) &&
        (!string.IsNullOrEmpty(_configuration.Value["Dex:FreeLLM:Endpoint"]) || !string.IsNullOrEmpty(_configuration.Value["Dex:FreeLLM:EmbeddingEndpoint"]));

    private static ServiceProvider BuildServices() {
        var services = new ServiceCollection();
        services.AddSingleton(_configuration.Value);
        services.AddLogging();
        new AgentsModelProviderBuilder(services).UseFreeLLM();
        return services.BuildServiceProvider();
    }

    [Fact(SkipUnless = nameof(HasChatEndpoint), Skip = "Integration test - requires Dex:FreeLLM:Endpoint and Dex:FreeLLM:Models:Reasoning in the user secrets")]
    public async Task The_endpoint_answers_a_chat_request() {
        using var services = BuildServices();
        var client = services.GetServices<IAgentsModelProvider>().Single().CreateChatClient(AgentsModelRole.Reasoning);

        var response = await client.GetResponseAsync("Reply with the single word: pong", cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(response.Text));
    }

    [Theory(SkipUnless = nameof(HasChatEndpoint), Skip = "Integration test - requires Dex:FreeLLM:Endpoint and Dex:FreeLLM:Models:Reasoning in the user secrets")]
    [InlineData(FreeLLMStructuredOutput.JsonSchema)]
    [InlineData(FreeLLMStructuredOutput.JsonObject)]
    public async Task The_endpoint_returns_a_typed_reply(FreeLLMStructuredOutput structuredOutput) {
        var services = new ServiceCollection();
        services.AddSingleton(_configuration.Value);
        services.AddLogging();
        new AgentsModelProviderBuilder(services).UseFreeLLM(options => options.Compatibility.StructuredOutput = structuredOutput);
        await using var provider = services.BuildServiceProvider();
        var agent = provider.GetServices<IAgentsModelProvider>().Single().CreateChatClient(AgentsModelRole.Reasoning)
            .AsAIAgent(options: new ChatClientAgentOptions { ChatOptions = new ChatOptions { Instructions = "Extract the person from the text.", Temperature = 0 }, Name = "LiveTest" });

        var response = await agent.RunAsync<Person>("Maria is 34 years old.", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("Maria", response.Result.Name, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(34, response.Result.Age);
    }

    [Fact(SkipUnless = nameof(HasEmbeddingEndpoint), Skip = "Integration test - requires Dex:FreeLLM:Models:Embedding (and an endpoint) in the user secrets")]
    public async Task The_endpoint_returns_vectors_the_knowledge_store_can_hold() {
        using var services = BuildServices();
        var generator = services.GetServices<IAgentsModelProvider>().Single().CreateEmbeddingGenerator();

        var embeddings = await generator.GenerateAsync(["a short sentence"], cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(AgentsOptions.EmbeddingDimensionsDefault, Assert.Single(embeddings).Vector.Length);
    }

    private sealed record Person(string Name, int Age);
}
