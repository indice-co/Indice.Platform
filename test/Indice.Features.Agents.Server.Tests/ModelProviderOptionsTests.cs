using Indice.Features.Agents.Core;
using Indice.Features.Agents.Core.Providers;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Server.Tests;

public class ModelProviderOptionsTests
{
    [Fact]
    public void ConfigureModelProviders_is_passed_to_the_agents_core() {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        services.AddAgents(configuration, options => {
            options.ConfigureModelProviders = providers => providers.Services.AddSingleton<IAgentsModelProvider>(new AllRolesProvider());
        });

        using var provider = services.BuildServiceProvider();
        // No Dex:AzureOpenAI settings at all: the registered provider serves every role, so the options validate.
        Assert.NotNull(provider.GetRequiredService<IOptions<AgentsOptions>>().Value);
        var reasoning = provider.GetRequiredKeyedService<IChatClient>(nameof(AgentsModelRole.Reasoning));
        Assert.Equal("all-roles", reasoning.GetService<ChatClientMetadata>()?.DefaultModelId);
    }

    private sealed class AllRolesProvider : IAgentsModelProvider
    {
        public string Name => "AllRoles";
        public bool Serves(AgentsModelRole role) => true;
        public IChatClient CreateChatClient(AgentsModelRole role) => new MetadataChatClient();
        public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator() => throw new NotSupportedException();
    }

    private sealed class MetadataChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType == typeof(ChatClientMetadata) && serviceKey is null ? new ChatClientMetadata("fake", null, "all-roles") : null;
        public void Dispose() { }
    }
}
