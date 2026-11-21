using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Providers;

/// <summary>
/// The built-in Azure AI Foundry (Azure OpenAI) provider, configured from <see cref="AgentsOptions.AzureOpenAI"/>. It is the default:
/// it serves every <see cref="AgentsModelRole"/> that no registered <see cref="IAgentsModelProvider"/> claims, and is therefore never part of the
/// registered provider list.
/// </summary>
/// <remarks>The <see cref="AzureOpenAIClient"/> is resolved lazily so hosts that serve every role from another provider need no Azure settings.</remarks>
internal sealed class AzureAIFoundryModelProvider : IAgentsModelProvider
{
    private readonly IOptions<AgentsOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    public AzureAIFoundryModelProvider(IOptions<AgentsOptions> options, IServiceProvider serviceProvider) {
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public string Name => "AzureAIFoundry";

    public bool Serves(AgentsModelRole role) => true;

    public IChatClient CreateChatClient(AgentsModelRole role) {
        var deployments = _options.Value.AzureOpenAI.Deployments;
        var deployment = role switch {
            AgentsModelRole.Reasoning => deployments.Reasoning,
            AgentsModelRole.Fast => deployments.Fast,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Chat clients exist only for the Reasoning and Fast roles.")
        };
        return _serviceProvider.GetRequiredService<AzureOpenAIClient>().GetChatClient(deployment).AsIChatClient();
    }

    public IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator() {
        var options = _options.Value.AzureOpenAI;
        return _serviceProvider.GetRequiredService<AzureOpenAIClient>()
            .GetEmbeddingClient(options.Deployments.Embedding)
            .AsIEmbeddingGenerator(options.EmbeddingDimensions);
    }
}
