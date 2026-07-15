using Azure.AI.OpenAI;
using Indice.Features.Agents.Core.Workflows;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Indice.Features.Agents.Core.Services;

/// <inheritdoc />
public class AgentsFactory : IAgentsFactory
{
    private readonly AzureOpenAIClient _openAIClient;
    private readonly AgentsOptions _options;
    private readonly ModelsOptions _models;
    private readonly IPromptTemplateRenderer _prompts;
    private readonly SessionStoreChatHistoryProvider _historyProvider;
    private readonly UserClaimsAIContextProvider _userContext;

    /// <summary>Creates a new <see cref="AgentsFactory"/>.</summary>
    public AgentsFactory(
        AzureOpenAIClient openAIClient,
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models,
        IPromptTemplateRenderer prompts,
        SessionStoreChatHistoryProvider historyProvider,
        UserClaimsAIContextProvider userContext) {
        _openAIClient = openAIClient;
        _options = options.Value;
        _models = models.Value;
        _prompts = prompts;
        _historyProvider = historyProvider;
        _userContext = userContext;
    }

    /// <inheritdoc />
    public AIAgent Create(AgentDescriptor descriptor) {
        ArgumentNullException.ThrowIfNull(descriptor);
        var deployments = _options.AzureOpenAI.Deployments;
        // The role selects both the deployment name and the base ChatOptions; clone so the shared base is never mutated.
        var (model, chatOptions) = descriptor.Role switch {
            AgentModelRole.Reasoning => (deployments.Reasoning!, _models.BaseReasoningModelOptions.Clone()),
            AgentModelRole.Fast => (deployments.Fast!, _models.BaseFastModelOptions.Clone()),
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.Role, "Unknown agent model role."),
        };
        chatOptions.Instructions = _prompts.Render(descriptor.PromptTemplate, descriptor.PromptValues);
        descriptor.ConfigureChatOptions?.Invoke(chatOptions);

        var agentOptions = new ChatClientAgentOptions {
            ChatOptions = chatOptions,
            Name = descriptor.Name,
        };
        if (descriptor.IncludeChatHistory) {
            agentOptions.ChatHistoryProvider = _historyProvider;
        }
        if (descriptor.IncludeUserContext) {
            agentOptions.AIContextProviders = [_userContext];
        }
        return _openAIClient.GetChatClient(model).AsAIAgent(agentOptions);
    }
}
