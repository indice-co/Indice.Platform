using Indice.Features.Agents.Core.Providers;
using Indice.Features.Agents.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Tests;

public class ModelProviderTests
{
    private static Dictionary<string, string?> AzureConfiguration(bool reasoning = true) => new() {
        ["Dex:AzureOpenAI:Endpoint"] = "https://example.openai.azure.com",
        ["Dex:AzureOpenAI:ApiKey"] = "secret",
        ["Dex:AzureOpenAI:Deployments:Reasoning"] = reasoning ? "azure-reasoning" : null,
        ["Dex:AzureOpenAI:Deployments:Fast"] = "azure-fast",
        ["Dex:AzureOpenAI:Deployments:Embedding"] = "azure-embedding",
    };

    private static ServiceProvider BuildServices(Dictionary<string, string?> settings, Action<AgentsModelProviderBuilder>? configureModelProviders = null) {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddAgentsCore(configuration, configureModelProviders: configureModelProviders);
        return services.BuildServiceProvider();
    }

    private static string? ModelOf(IChatClient client) => client.GetService<ChatClientMetadata>()?.DefaultModelId;

    [Fact]
    public void Azure_AI_Foundry_serves_every_role_by_default() {
        using var provider = BuildServices(AzureConfiguration());

        Assert.Equal("azure-reasoning", ModelOf(provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Reasoning))));
        Assert.Equal("azure-fast", ModelOf(provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Fast))));
        var metadata = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>().GetService<EmbeddingGeneratorMetadata>();
        Assert.Equal("azure-embedding", metadata?.DefaultModelId);
        Assert.Equal(AgentsOptions.EmbeddingDimensionsDefault, metadata?.DefaultModelDimensions);
    }

    [Fact]
    public void Validator_without_providers_requires_the_azure_settings() {
        var result = new AgentsOptionsValidator().Validate(null, new AgentsOptions());

        Assert.True(result.Failed);
        Assert.Equal([
            "Dex:AzureOpenAI:Endpoint must be configured (e.g. via dotnet user-secrets).",
            "Dex:AzureOpenAI:ApiKey must be configured (e.g. via dotnet user-secrets).",
            "Dex:AzureOpenAI:Deployments:Reasoning must be configured.",
            "Dex:AzureOpenAI:Deployments:Fast must be configured.",
            "Dex:AzureOpenAI:Deployments:Embedding must be configured.",
        ], result.Failures);
    }

    [Fact]
    public void A_provider_serving_every_role_needs_no_azure_settings() {
        var fake = new FakeProvider("Fake", AgentsModelRole.Reasoning, AgentsModelRole.Fast, AgentsModelRole.Embedding);
        using var provider = BuildServices([], builder => builder.Services.AddSingleton<IAgentsModelProvider>(fake));

        // Resolving the options runs the validator, which in turn resolves the providers: this also proves the graph has no cycle.
        var options = provider.GetRequiredService<IOptions<AgentsOptions>>().Value;
        Assert.NotNull(options);
        Assert.Equal("Fake-Reasoning", ModelOf(provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Reasoning))));
        Assert.Equal("Fake-Fast", ModelOf(provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Fast))));
        Assert.Equal("Fake-Embedding", provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>().GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId);
    }

    [Fact]
    public async Task Intent_routing_does_not_need_azure_settings() {
        var fake = new FakeProvider("Fake", AgentsModelRole.Reasoning, AgentsModelRole.Fast, AgentsModelRole.Embedding);
        using var provider = BuildServices([], builder => builder.Services.AddSingleton<IAgentsModelProvider>(fake));

        var systemOne = provider.GetRequiredService<ISystemOneClient>();

        var decision = await systemOne.DecideAsync(new Models.SystemOneDecisionRequest(), TestContext.Current.CancellationToken);
        Assert.Empty(decision.Answers);
    }

    [Fact]
    public void Roles_no_provider_claims_fall_back_to_azure_and_only_those_need_azure_settings() {
        var fake = new FakeProvider("Fake", AgentsModelRole.Reasoning);
        // No Azure Reasoning deployment: the fake serves that role.
        using var provider = BuildServices(AzureConfiguration(reasoning: false), builder => builder.Services.AddSingleton<IAgentsModelProvider>(fake));

        Assert.NotNull(provider.GetRequiredService<IOptions<AgentsOptions>>().Value);
        Assert.Equal("Fake-Reasoning", ModelOf(provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Reasoning))));
        Assert.Equal("azure-fast", ModelOf(provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Fast))));
        Assert.Equal("azure-embedding", provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>().GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId);
    }

    [Fact]
    public void Unclaimed_roles_still_require_the_azure_settings() {
        var fake = new FakeProvider("Fake", AgentsModelRole.Reasoning);
        using var provider = BuildServices([], builder => builder.Services.AddSingleton<IAgentsModelProvider>(fake));

        var exception = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<AgentsOptions>>().Value);

        Assert.Contains(exception.Failures, failure => failure.StartsWith("Dex:AzureOpenAI:Endpoint"));
        Assert.Contains(exception.Failures, failure => failure.StartsWith("Dex:AzureOpenAI:ApiKey"));
        Assert.Contains(exception.Failures, failure => failure.StartsWith("Dex:AzureOpenAI:Deployments:Fast"));
        Assert.Contains(exception.Failures, failure => failure.StartsWith("Dex:AzureOpenAI:Deployments:Embedding"));
        Assert.DoesNotContain(exception.Failures, failure => failure.StartsWith("Dex:AzureOpenAI:Deployments:Reasoning"));
    }

    [Fact]
    public void A_role_claimed_by_two_providers_is_rejected() {
        var first = new FakeProvider("First", AgentsModelRole.Reasoning);
        var second = new FakeProvider("Second", AgentsModelRole.Reasoning);

        var result = new AgentsOptionsValidator([first, second]).Validate(null, new AgentsOptions());
        Assert.Contains(result.Failures!, failure => failure.Contains("'Reasoning'") && failure.Contains("First, Second"));

        using var provider = BuildServices(AzureConfiguration(), builder => {
            builder.Services.AddSingleton<IAgentsModelProvider>(first);
            builder.Services.AddSingleton<IAgentsModelProvider>(second);
        });
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Reasoning)));
    }

    [Fact]
    public void The_system_one_model_needs_an_endpoint_and_key_when_enabled() {
        var fake = new FakeProvider("Fake", AgentsModelRole.Reasoning, AgentsModelRole.Fast, AgentsModelRole.Embedding);
        var options = new AgentsOptions();
        options.SystemOne.Enabled = true;
        options.AzureOpenAI.Deployments.SystemOne = "Decision-1";

        var result = new AgentsOptionsValidator([fake]).Validate(null, options);

        Assert.Contains(result.Failures!, failure => failure.StartsWith("Dex:AzureOpenAI:SystemOneEndpoint"));
        Assert.Contains(result.Failures!, failure => failure.StartsWith("Dex:AzureOpenAI:SystemOneApiKey"));

        options.AzureOpenAI.SystemOneEndpoint = "https://decisions.example.com";
        options.AzureOpenAI.SystemOneApiKey = "secret";
        Assert.True(new AgentsOptionsValidator([fake]).Validate(null, options).Succeeded);
    }

    [Fact]
    public void Adding_the_same_provider_type_twice_registers_it_once() {
        var services = new ServiceCollection();
        var builder = new AgentsModelProviderBuilder(services);

        builder.AddModelProvider<ParameterlessProvider>().AddModelProvider<ParameterlessProvider>();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IAgentsModelProvider));
    }

    private sealed class ParameterlessProvider : IAgentsModelProvider
    {
        public string Name => "Parameterless";
        public bool Serves(AgentsModelRole role) => false;
        public IChatClient CreateChatClient(AgentsModelRole role) => throw new NotSupportedException();
        public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator() => throw new NotSupportedException();
    }

    private sealed class FakeProvider(string name, params AgentsModelRole[] roles) : IAgentsModelProvider
    {
        public string Name { get; } = name;
        public bool Serves(AgentsModelRole role) => roles.Contains(role);
        public IChatClient CreateChatClient(AgentsModelRole role) => new FakeChatClient($"{Name}-{role}");
        public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator() => new FakeEmbeddingGenerator($"{Name}-{AgentsModelRole.Embedding}");
    }

    private sealed class FakeChatClient(string model) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType == typeof(ChatClientMetadata) && serviceKey is null ? new ChatClientMetadata("fake", null, model) : null;
        public void Dispose() { }
    }

    private sealed class FakeEmbeddingGenerator(string model) : IEmbeddingGenerator<string, Embedding<float>>
    {
        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType == typeof(EmbeddingGeneratorMetadata) && serviceKey is null ? new EmbeddingGeneratorMetadata("fake", null, model, AgentsOptions.EmbeddingDimensionsDefault) : null;
        public void Dispose() { }
    }
}
