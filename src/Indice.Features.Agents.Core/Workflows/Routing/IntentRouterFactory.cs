using Indice.Features.Agents.Core.Workflows.Prompts;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Routing;

/// <summary>
/// Builds the master intent router as a Microsoft Agent Framework handoff orchestration: a triage agent that reads the latest user
/// message in the context of the conversation and hands the turn off, through a tool call, to exactly one registered agent.
/// Targets are every <see cref="AgentDefinition"/> in the catalogue that is not itself a router.
/// </summary>
public static class IntentRouterFactory
{
    /// <summary>
    /// Creates the handoff workflow for the current request: router → one of the registered target agents. There are no
    /// return handoffs and no autonomous continuation, so a turn ends as soon as the chosen agent answers.
    /// </summary>
    /// <exception cref="InvalidOperationException">No routable agent is registered.</exception>
    public static Workflow CreateHandoffWorkflow(IServiceProvider serviceProvider) {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var targets = serviceProvider.GetServices<AgentDefinition>().Where(definition => !definition.IsRouter).ToList();
        if (targets.Count == 0) {
            throw new InvalidOperationException("No routable agents are registered. Register at least one agent with AddAgent or AddAgentWorkflow before using the intent router.");
        }
        var router = CreateRouterAgent(serviceProvider, targets);
        var agents = targets.Select(definition => definition.CreateAgent!(serviceProvider)).ToList();
        return AgentWorkflowBuilder.CreateHandoffBuilderWith(router)
            .WithHandoffs(router, agents)
            .Build();
    }

    /// <summary>
    /// Creates the triage agent on the fast deployment. Its instructions list every target (name + description) rendered from the
    /// <c>IntentRouter</c> prompt template, it sees the conversation history through <see cref="ConversationStoreChatHistoryProvider"/>,
    /// and it is forced to call a tool so it can never answer in prose.
    /// </summary>
    public static AIAgent CreateRouterAgent(IServiceProvider serviceProvider, IReadOnlyList<AgentDefinition> targets) {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(targets);
        var chatClient = serviceProvider.GetRequiredKeyedService<IChatClient>(nameof(AgentsOptions.AzureOpenAIDeployments.Fast));
        var models = serviceProvider.GetRequiredService<IOptions<ModelsOptions>>().Value;
        var prompts = serviceProvider.GetRequiredService<IPromptTemplateRenderer>();
        var historyProvider = serviceProvider.GetRequiredService<ConversationStoreChatHistoryProvider>();

        var defaultTarget = targets.FirstOrDefault(definition => string.Equals(definition.Name, AgentsConstants.AgentNames.Knowledge, StringComparison.OrdinalIgnoreCase))
            ?? targets[0];
        var chatOptions = models.BaseFastModelOptions.Clone();
        chatOptions.Instructions = prompts.Render(nameof(AgentsConstants.PromptDefaults.IntentRouter), new {
            agents = targets.Select(definition => new { name = definition.Name, description = definition.Description }).ToList(),
            defaultTarget = defaultTarget.Name,
        });
        // The handoff tools are injected per run by the handoff executor; requiring a tool call keeps the router from replying in text.
        chatOptions.ToolMode = ChatToolMode.RequireAny;

        return chatClient.AsAIAgent(options: new ChatClientAgentOptions() {
            Id = AgentsConstants.IntentRouter.AgentId,
            Name = AgentsConstants.IntentRouter.AgentName,
            Description = "Routes each turn to the agent best suited to answer it.",
            ChatOptions = chatOptions,
            ChatHistoryProvider = historyProvider,
            // Chat completions is stateless; the echoed request ConversationId must not be treated as server-side history.
            ThrowOnChatHistoryProviderConflict = false,
        });
    }
}
