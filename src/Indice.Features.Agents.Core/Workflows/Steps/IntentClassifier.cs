using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Events;
using Indice.Features.Agents.Core.Workflows.Prompts;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Classifies the user's question (intent + category + language) and decides whether it is in scope.
/// On out-of-scope the workflow routes to <see cref="OutOfScopeResponder"/> via a conditional edge; otherwise
/// downstream steps receive validated <see cref="Intent"/> and <see cref="RetrievalFilters"/>.
/// </summary>
public sealed class IntentClassifier : Executor<ConversationState, IntentOutput>
{
    private readonly AIAgent _agent;
    private readonly AgentsOptions _options;
    private readonly string _model;

    /// <summary>Creates a new <see cref="IntentClassifier"/>.</summary>
    public IntentClassifier(
        [FromKeyedServices(nameof(AgentsOptions.AzureOpenAIDeployments.Reasoning))] IChatClient chatClient, 
        IOptions<AgentsOptions> options,
        IOptions<ModelsOptions> models, IPromptTemplateRenderer prompts,
        SessionStoreChatHistoryProvider historyProvider) : base("IntentClassifier") {
        _options = options.Value;
        _model = _options.AzureOpenAI.Deployments.Reasoning!;
        var chatOptions = models.Value.BaseReasoningModelOptions.Clone();
        chatOptions.Instructions = prompts.Render("IntentClassifier", new {
            categories = _options.Taxonomy.Categories,
            languages = _options.Taxonomy.Languages,
        });
        _agent = chatClient.AsAIAgent(options: new ChatClientAgentOptions() {
                ChatOptions = chatOptions,
                Name = "DexIntentClassifier",
                ChatHistoryProvider = historyProvider,
            });
    }

    /// <inheritdoc/>
    public override async ValueTask<IntentOutput> HandleAsync(
        ConversationState message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        var question = message.Message.Text; 
        var conversationId = message.ConversationId;
        await context.SetConversationStateAsync(message, cancellationToken);
        var agentSession = await _agent.CreateSessionAsync(cancellationToken);
        
        SessionStoreChatHistoryProvider.SetSessionId(agentSession, Guid.Parse(conversationId));
        var response = await _agent.RunAsync<IntentResult>(question, agentSession, cancellationToken: cancellationToken);
        if (response.Usage is not null) {
            await context.AddEventAsync(new UsageEvent(response.Usage, _model), cancellationToken);
        }
        var result = response.Result;

        var category = _options.Taxonomy.Categories.Contains(result.Category ?? "", StringComparer.OrdinalIgnoreCase) ? result.Category : null;
        var language = _options.Taxonomy.Languages.Contains(result.Language ?? "", StringComparer.OrdinalIgnoreCase) ? result.Language : null;

        var intent = new Intent {
            Type = string.IsNullOrWhiteSpace(result.Type) ? "question" : result.Type,
            Category = category,
            Language = language,
            IsInScope = result.IsInScope,
            OutOfScopeReason = result.OutOfScopeReason,
        };
        await context.SetIntentStateAsync(new IntentState(intent, new RetrievalFilters { Category = category, Language = language }), cancellationToken);
        return new IntentOutput (
            intent,
            new RetrievalFilters { Category = category, Language = language }
        );
    }

    private sealed class IntentResult
    {
        public string? Type { get; set; }
        public string? Category { get; set; }
        public string? Language { get; set; }
        public bool IsInScope { get; set; }
        public string? OutOfScopeReason { get; set; }
    }
}
/// <summary>Output payload of <c>IntentClassifier</c>.</summary>
/// <param name="Intent">The classified intent.</param>
/// <param name="Filters">Retrieval filters derived from the intent (category, language).</param>
public record IntentOutput(Intent Intent, RetrievalFilters Filters);